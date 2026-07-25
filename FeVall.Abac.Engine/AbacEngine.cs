// FeVall.Abac.Engine/AbacEngine.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Audit;
using FeVall.Abac.Engine.Audit;

namespace FeVall.Abac.Engine;

/// <summary>
/// Orquestador principal del motor ABAC.
/// SRP: coordina el flujo — evaluación, logging y auditoría — pero no implementa
/// ninguna de las tres cosas, delega a las abstracciones correspondientes.
/// internal sealed: el consumidor nunca instancia esta clase directamente.
/// </summary>
internal sealed class AbacEngine : IAbacEngine
{
    private readonly IPolicyEvaluator _evaluator;
    private readonly IAbacLogger _logger;
    private readonly IAuditSink _auditSink;

    public AbacEngine(IPolicyEvaluator evaluator, IAbacLogger logger, IAuditSink auditSink)
    {
        _evaluator = evaluator;
        _logger = logger;
        _auditSink = auditSink;
    }

    public async Task<Decision> EvaluateAsync(
       IEvaluationContext context,
       CancellationToken ct = default)
    {
        _logger.LogEvaluationStarted(context);

        var decision = await _evaluator.EvaluateAsync(context, ct);

        _logger.LogDecisionReached(context, decision);

        // Fail-open para auditoría: un problema al ENCOLAR el registro (ej. canal
        // saturado lanzando en una implementación distinta a ChannelAuditSink,
        // o cualquier IAuditSink de terceros mal escrito) NUNCA debe convertir
        // una Decision ya calculada en una excepción hacia el consumidor.
        // La autorización es fail-closed; la auditoría, deliberadamente, no lo es —
        // perder un registro de auditoría es preferible a denegar acceso legítimo
        // por un fallo del subsistema de logging.
        await TryWriteAuditEntryAsync(context, decision, ct);

        return decision;
    }

    private async Task TryWriteAuditEntryAsync(
        IEvaluationContext context, Decision decision, CancellationToken ct)
    {
        try
        {
            var entry = AuditEntryFactory.Build(context, decision);
            await _auditSink.WriteAsync(entry, ct);
        }
        catch (OperationCanceledException)
        {
            // Cancelación del caller — no es un fallo de auditoría, se deja propagar
            // implícitamente al no capturarla más abajo... pero como esta decisión
            // ya se calculó y se va a retornar de todas formas, se ignora aquí:
            // no tiene sentido cancelar el retorno de una Decision ya obtenida
            // solo porque el registro de auditoría no llegó a encolarse a tiempo.
        }
        catch (Exception)
        {
            // Best-effort: se traga cualquier excepción del sink. Si en el futuro
            // se requiere visibilidad de estos fallos, IAbacLogger necesitaría un
            // método explícito para errores de infraestructura (no existe hoy,
            // y agregarlo infla su contrato con un caso muy poco frecuente).
        }
    }
}