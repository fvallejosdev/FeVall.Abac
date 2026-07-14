// FeVall.Abac.Abstractions/IAbacLogger.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Contrato de logging específico del motor ABAC.
/// DIP: el motor depende de esta abstracción, nunca de ILogger&lt;T&gt; de Microsoft.
/// ISP: métodos con nombres de dominio — no un logger genérico de propósito general.
/// </summary>
public interface IAbacLogger
{
    /// <summary>
    /// Registra el inicio de una evaluación.
    /// Llamado por AbacEngine antes de delegar al evaluador.
    /// </summary>
    void LogEvaluationStarted(IEvaluationContext context);

    /// <summary>
    /// Registra la decisión final alcanzada.
    /// Llamado por AbacEngine después de recibir la decisión del evaluador.
    /// </summary>
    void LogDecisionReached(IEvaluationContext context, Decision decision);

    /// <summary>
    /// Registra cuando una política individual es evaluada.
    /// Llamado por PolicyEvaluator por cada política procesada.
    /// </summary>
    void LogPolicyEvaluated(IPolicy policy, Decision decision);

    /// <summary>
    /// Registra cuando una política no aplica al contexto dado.
    /// Llamado por PolicyEvaluator cuando IPolicyApplicability retorna false.
    /// </summary>
    void LogPolicySkipped(IPolicy policy, IEvaluationContext context);
}