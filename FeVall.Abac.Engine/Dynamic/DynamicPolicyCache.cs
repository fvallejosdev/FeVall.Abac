// FeVall.Abac.Engine/Dynamic/DynamicPolicyCache.cs
using System.Collections.Concurrent;
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// IPolicyProvider respaldado por caché en memoria (ConcurrentDictionary) que se
    /// mantiene sincronizada entre réplicas/pods vía IPolicyChangeNotifier (Redis
    /// Pub/Sub, PostgreSQL LISTEN/NOTIFY, etc.). Cuando la UI publica un cambio en
    /// el servidor A, esta clase en los servidores B y C recompila SOLO la política
    /// afectada y reemplaza la entrada — sin bloquear ni reiniciar el proceso.
    /// Cada política se envuelve en FaultTolerantPolicyDecorator antes de cachearse:
    /// el aislamiento de fallos ocurre una vez, no en cada lectura.
    /// internal sealed: se expone al consumidor únicamente como IPolicyProvider.
    /// </summary>
    internal sealed class DynamicPolicyCache : IPolicyProvider, IAsyncDisposable
    {
        private readonly IPolicyRepository _repository;
        private readonly IPolicyCompiler _compiler;
        private readonly IAbacLogger _logger;
        private readonly ConcurrentDictionary<string, IPolicy> _cache = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private readonly CancellationTokenSource _listenerCts = new();
        private readonly Task _listenerTask;
        private volatile bool _isLoaded;

        private static readonly TimeSpan MinReconnectDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

        public DynamicPolicyCache(
            IPolicyRepository repository,
            IPolicyCompiler compiler,
            IAbacLogger logger,
            IPolicyChangeNotifier notifier)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(compiler);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(notifier);

            _repository = repository;
            _compiler = compiler;
            _logger = logger;

            // Suscripción de fondo — vive mientras viva la caché, no mientras vive un request.
            _listenerTask = ListenForInvalidationsAsync(notifier, _listenerCts.Token);
        }

        public async Task<IReadOnlyList<IPolicy>> GetPoliciesAsync(CancellationToken ct = default)
        {
            await EnsureLoadedAsync(ct);

            // Snapshot: .Values sobre ConcurrentDictionary es seguro para lectura concurrente
            // mientras otro hilo escribe (ej. una invalidación llegando en simultáneo).
            return _cache.Values.ToArray();
        }

        private async Task EnsureLoadedAsync(CancellationToken ct)
        {
            if (_isLoaded) return;

            await _loadLock.WaitAsync(ct);
            try
            {
                if (_isLoaded) return;

                var definitions = await _repository.GetAllAsync(ct);
                foreach (var definition in definitions)
                    TryCompileAndCache(definition);

                _isLoaded = true;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        private void TryCompileAndCache(PolicyDefinition definition)
        {
            try
            {
                _cache[definition.PolicyId] = Wrap(_compiler.Compile(definition));
            }
            catch (PolicyCompilationException ex)
            {
                // Aislamiento a nivel de CARGA: una política mal escrita en la UI
                // no debe impedir que las demás políticas válidas se activen.
                _logger.LogPolicySkipped(
                    new UncompilablePolicyPlaceholder(definition.PolicyId),
                    EmptyContextForLogging.Instance);
                _ = ex;
            }
            catch (Exception ex)
            {
                // Ensanchado deliberadamente más allá de PolicyCompilationException:
                // un operador custom mal implementado, o cualquier fallo inesperado
                // durante Compile(), NO debe tumbar la carga completa de EnsureLoadedAsync.
                // Mismo criterio de aislamiento fail-safe que FaultTolerantPolicyDecorator
                // aplica en tiempo de EVALUACIÓN — aquí se aplica en tiempo de COMPILACIÓN/CARGA.
                // Se registra como fallo de infraestructura porque, a diferencia de un
                // PolicyCompilationException (error de negocio esperable del usuario en la UI),
                // esto es un fallo técnico no previsto que sí amerita visibilidad por defecto.
                _logger.LogInfrastructureFault(
                    $"{nameof(DynamicPolicyCache)}.{nameof(TryCompileAndCache)}({definition.PolicyId})", ex);
            }
        }

        private IPolicy Wrap(IPolicy compiled) => new FaultTolerantPolicyDecorator(compiled, _logger);

        /// <summary>
        /// Se suscribe a invalidaciones y, si el STREAM COMPLETO se cae (no solo
        /// el manejo de un policyId individual — eso ya lo cubre el try/catch
        /// interno), reintenta la suscripción con backoff exponencial acotado en
        /// vez de dejar morir la tarea de fondo silenciosamente. Sin esto, un pod
        /// que pierde la conexión a la infraestructura de pub/sub (Redis, etc.)
        /// queda sordo a invalidaciones de forma PERMANENTE hasta un reinicio manual —
        /// justo el escenario que IPolicyChangeNotifier existe para resolver.
        /// </summary>
        private async Task ListenForInvalidationsAsync(IPolicyChangeNotifier notifier, CancellationToken ct)
        {
            var delay = MinReconnectDelay;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await foreach (var policyId in notifier.Subscribe(ct).WithCancellation(ct))
                    {
                        delay = MinReconnectDelay; // Conexión sana: se resetea el backoff.

                        try
                        {
                            var fresh = await _repository.GetByIdAsync(policyId, ct);

                            if (fresh is null)
                                _cache.TryRemove(policyId, out _);
                            else
                                TryCompileAndCache(fresh);
                        }
                        catch (Exception) when (!ct.IsCancellationRequested)
                        {
                            // Fail-safe: si la recompilación de ESTA política falla,
                            // se conserva la versión anterior en caché — no afecta
                            // al stream de suscripción en sí.
                        }
                    }

                    // El stream terminó sin excepción (ej. el servidor cerró la
                    // conexión de forma ordenada) — se trata igual que una caída:
                    // se reintenta, porque el listener debe vivir mientras viva la caché.
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return; // Apagado normal del servicio — no es un error.
                }
                catch (Exception ex)
                {
                    _logger.LogInfrastructureFault(nameof(DynamicPolicyCache), ex);
                }

                if (ct.IsCancellationRequested) return;

                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // Backoff exponencial acotado: 1s, 2s, 4s, 8s, 16s, 30s, 30s...
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxReconnectDelay.TotalSeconds));
            }
        }

        public async ValueTask DisposeAsync()
        {
            _listenerCts.Cancel();
            try { await _listenerTask; } catch (OperationCanceledException) { }
            _listenerCts.Dispose();
        }

        // Placeholder mínimo para poder reutilizar IAbacLogger.LogPolicySkipped al reportar
        // una política que ni siquiera llegó a compilar (no existe un IPolicy real todavía).
        private sealed class UncompilablePolicyPlaceholder : IPolicy
        {
            public string Name { get; }
            public UncompilablePolicyPlaceholder(string policyId) => Name = policyId;
            public Task<Decision> EvaluateAsync(IEvaluationContext context, CancellationToken ct = default) =>
                throw new InvalidOperationException("Esta política nunca compiló y no debe evaluarse.");
        }

        private sealed class EmptyContextForLogging : IEvaluationContext
        {
            public static readonly EmptyContextForLogging Instance = new();
            public AttributeBag Subject { get; } = new();
            public AttributeBag Resource { get; } = new();
            public AttributeBag Action { get; } = new();
            public AttributeBag Environment { get; } = new();
        }
    }
}