// FeVall.Abac.Engine/Dynamic/PolicyPublishingService.cs
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// Único punto de entrada para que la UI publique o revierta una política.
    /// SRP: coordina compilación de validación + versionado con reintento optimista +
    /// invalidación de caché; no implementa ninguna de las tres cosas, delega a las
    /// abstracciones correspondientes.
    /// Invariante de diseño: NUNCA se publica una PolicyDefinition que no compile —
    /// esto es más estricto que el sandbox (que permite probar cosas rotas);
    /// aquí ya estamos en el camino de producción.
    /// public: el consumidor de la librería la instancia/inyecta directamente en su
    /// capa de administración (a diferencia de la mayoría de las clases de Engine,
    /// que son internal por diseño).
    /// </summary>
    public sealed class PolicyPublishingService
    {
        private const int MaxConflictRetries = 3;

        private readonly IPolicyVersionStore _versionStore;
        private readonly IPolicyCompiler _compiler;
        private readonly IPolicyChangeNotifier _notifier;

        public PolicyPublishingService(
            IPolicyVersionStore versionStore,
            IPolicyCompiler compiler,
            IPolicyChangeNotifier notifier)
        {
            _versionStore = versionStore;
            _compiler = compiler;
            _notifier = notifier;
        }

        /// <summary>
        /// Publica una nueva versión. Lanza PolicyCompilationException si la definición
        /// no compila — la UI debe haber pasado por IPolicySandbox antes de llegar aquí,
        /// pero esta es la última línea de defensa antes de tocar producción.
        /// </summary>
        public async Task<PolicyVersion> PublishAsync(
            PolicyDefinition definition,
            string? publishedBy = null,
            string? changeNote = null,
            CancellationToken ct = default)
        {
            _compiler.Compile(definition);

            return await AppendWithRetryAsync(
                () => new PolicyVersion
                {
                    PolicyId = definition.PolicyId,
                    Definition = definition,
                    PublishedAtUtc = DateTimeOffset.UtcNow,
                    PublishedBy = publishedBy,
                    ChangeNote = changeNote,
                    Version = 0 // se completa dentro de AppendWithRetryAsync
                },
                definition.PolicyId,
                ct);
        }

        /// <summary>
        /// Revierte a una versión anterior. NO borra ni modifica historial —
        /// crea una versión NUEVA cuyo contenido es una copia exacta de la versión
        /// objetivo. El historial completo queda intacto para auditoría.
        /// </summary>
        public async Task<PolicyVersion> RevertToAsync(
            string policyId,
            int targetVersion,
            string? revertedBy = null,
            CancellationToken ct = default)
        {
            var target = await _versionStore.GetVersionAsync(policyId, targetVersion, ct)
                ?? throw new InvalidOperationException(
                    $"La política '{policyId}' no tiene una versión {targetVersion} en su historial.");

            // El whitelist de operadores pudo haber cambiado desde la publicación
            // original — se revalida antes de reintentar el append.
            _compiler.Compile(target.Definition);

            return await AppendWithRetryAsync(
                () => new PolicyVersion
                {
                    PolicyId = policyId,
                    Definition = target.Definition,
                    PublishedAtUtc = DateTimeOffset.UtcNow,
                    PublishedBy = revertedBy,
                    ChangeNote = $"Revertido a la versión {targetVersion}.",
                    RevertedFromVersion = targetVersion,
                    Version = 0
                },
                policyId,
                ct);
        }

        public Task<IReadOnlyList<PolicyVersion>> GetHistoryAsync(string policyId, CancellationToken ct = default) =>
            _versionStore.GetHistoryAsync(policyId, ct);

        /// <summary>
        /// Calcula la próxima versión y hace AppendAsync. Si otra publicación
        /// concurrente ganó la carrera (PolicyVersionConflictException), vuelve
        /// a leer GetLatestAsync y reintenta con la versión recalculada — hasta
        /// MaxConflictRetries veces. Si el límite se agota, se deja que la excepción
        /// suba en el intento final — un choque tan persistente probablemente indica
        /// un problema más profundo (bug en el store, loop de reintento del cliente,
        /// etc.) que no debe ocultarse reintentando indefinidamente.
        /// </summary>
        private async Task<PolicyVersion> AppendWithRetryAsync(
            Func<PolicyVersion> buildVersionWithoutNumber,
            string policyId,
            CancellationToken ct)
        {
            for (var attempt = 1; attempt <= MaxConflictRetries; attempt++)
            {
                var nextVersion = await ComputeNextVersionAsync(policyId, ct);
                var version = buildVersionWithoutNumber() with { Version = nextVersion };

                try
                {
                    await _versionStore.AppendAsync(version, ct);
                    await _notifier.PublishInvalidationAsync(policyId, ct);
                    return version;
                }
                catch (PolicyVersionConflictException) when (attempt < MaxConflictRetries)
                {
                    // Otra publicación/revert concurrente ganó la versión N —
                    // se recalcula contra el estado actual y se reintenta.
                }
            }

            var finalVersion = await ComputeNextVersionAsync(policyId, ct);
            var finalAttempt = buildVersionWithoutNumber() with { Version = finalVersion };
            await _versionStore.AppendAsync(finalAttempt, ct);
            await _notifier.PublishInvalidationAsync(policyId, ct);
            return finalAttempt;
        }

        private async Task<int> ComputeNextVersionAsync(string policyId, CancellationToken ct)
        {
            var latest = await _versionStore.GetLatestAsync(policyId, ct);
            return (latest?.Version ?? 0) + 1;
        }
    }
}