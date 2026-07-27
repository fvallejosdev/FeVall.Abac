// FeVall.Abac.Engine/Logging/AbacConsoleLogger.cs
using FeVall.Abac.Abstractions;
using Microsoft.Extensions.Logging;

namespace FeVall.Abac.Engine.Logging;

/// <summary>
/// Implementación concreta de IAbacLogger usando ILogger&lt;T&gt; de Microsoft.
/// DIP: IAbacLogger (abstracción) no depende de Microsoft.Extensions.Logging.
///      Solo esta clase concreta lo hace — y vive en Engine, no en Abstractions.
/// internal sealed: detalle de implementación del motor.
/// </summary>
internal sealed class AbacConsoleLogger : IAbacLogger
{
    private readonly ILogger<AbacConsoleLogger> _logger;

    public AbacConsoleLogger(ILogger<AbacConsoleLogger> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public void LogEvaluationStarted(IEvaluationContext context)
    {
        if (!_logger.IsEnabled(LogLevel.Debug)) return;

        _logger.LogDebug(
            "[ABAC] Evaluación iniciada — Subject keys: {Keys}",
            string.Join(", ", context.Subject.Keys));
    }

    public void LogDecisionReached(IEvaluationContext context, Decision decision)
    {
        var level = decision.IsDeny ? LogLevel.Warning : LogLevel.Information;

        if (!_logger.IsEnabled(level)) return;

        _logger.Log(
            level,
            "[ABAC] Decisión final: {Effect} — Razón: {Reason}",
            decision.Effect,
            decision.Reason ?? "Sin razón especificada");
    }

    public void LogPolicyEvaluated(IPolicy policy, Decision decision)
    {
        if (!_logger.IsEnabled(LogLevel.Debug)) return;

        _logger.LogDebug(
            "[ABAC] Política evaluada: {Policy} → {Effect} — Razón: {Reason}",
            policy.Name,
            decision.Effect,
            decision.Reason ?? "Sin razón especificada");
    }

    public void LogPolicySkipped(IPolicy policy, IEvaluationContext context)
    {
        if (!_logger.IsEnabled(LogLevel.Debug)) return;

        _logger.LogDebug(
            "[ABAC] Política omitida: {Policy} — No aplica al contexto.",
            policy.Name);
    }

    public void LogInfrastructureFault(string component, Exception exception)
    {
        // Siempre Warning o superior — a diferencia del resto de los logs de este
        // logger (Debug), un fallo de infraestructura debe ser visible por defecto,
        // no requerir que el consumidor active logging detallado para enterarse.
        _logger.LogWarning(
            exception,
            "[ABAC] Fallo de infraestructura en {Component}: {Message}",
            component,
            exception.Message);
    }
}
