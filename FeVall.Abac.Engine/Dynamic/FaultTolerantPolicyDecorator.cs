// FeVall.Abac.Engine/Dynamic/FaultTolerantPolicyDecorator.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Decorator de IPolicy que garantiza aislamiento de fallos (Fault Isolation).
    /// Una política dinámica escrita desde la UI puede tener errores lógicos que solo
    /// se manifiestan en runtime (comparar "N/A" contra un GreaterThan, un atributo
    /// ausente, etc.). Esta clase asegura que NINGUNA excepción de una política
    /// se propague hacia AbacEngine — siempre se traduce en Deny (fail-closed).
    /// OCP: envuelve cualquier IPolicy sin modificarlo. Se aplica a TODA política
    /// dinámica en DynamicPolicyCache; las políticas estáticas escritas a mano por
    /// el equipo de desarrollo (que ya se asume revisadas en code review) no la requieren.
    /// internal sealed: detalle de implementación del motor.
    /// </summary>
    internal sealed class FaultTolerantPolicyDecorator : IPolicy, IPolicyApplicability
    {
        private readonly IPolicy _inner;
        private readonly IAbacLogger _logger;

        public string Name => _inner.Name;

        public FaultTolerantPolicyDecorator(IPolicy inner, IAbacLogger logger)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(logger);
            _inner = inner;
            _logger = logger;
        }

        public bool IsApplicableTo(IEvaluationContext context)
        {
            if (_inner is not IPolicyApplicability applicability)
                return true;

            try
            {
                return applicability.IsApplicableTo(context);
            }
            catch (Exception ex)
            {
                // Fail-safe: si el Target no puede evaluarse, la política se trata
                // como NO aplicable en lugar de arriesgar una excepción hacia el evaluador.
                LogFault("IsApplicableTo", ex);
                return false;
            }
        }

        public async Task<Decision> EvaluateAsync(IEvaluationContext context, CancellationToken ct = default)
        {
            try
            {
                return await _inner.EvaluateAsync(context, ct);
            }
            catch (OperationCanceledException)
            {
                throw; // La cancelación del caller no es un fallo de la política — debe propagar.
            }
            catch (Exception ex)
            {
                // Fail-Closed: la razón técnica se registra internamente para diagnóstico
                // pero NUNCA se expone en la Decision pública (evita filtrar detalles del sistema).
                LogFault("EvaluateAsync", ex);
                return Decision.DenyWith($"La política '{Name}' falló durante su evaluación y fue denegada por seguridad.");
            }
        }

        private void LogFault(string phase, Exception ex) =>
            _logger.LogPolicyEvaluated(
                _inner,
                Decision.DenyWith($"[FaultIsolation:{phase}] {ex.GetType().Name}: {ex.Message}"));
    }
}
