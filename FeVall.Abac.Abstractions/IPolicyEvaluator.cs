// FeVall.Abac.Abstractions/IPolicyEvaluator.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Evalúa una colección de políticas contra un contexto dado
/// y retorna una decisión combinada.
/// SRP: su única responsabilidad es coordinar la evaluación de políticas.
///      No combina resultados — eso es ICombinationStrategy.
///      No valida el contexto — eso es NullGuardPolicyEvaluator (decorator).
///      No orquesta el flujo completo — eso es IAbacEngine.
/// </summary>
public interface IPolicyEvaluator
{
    /// <summary>
    /// Evalúa todas las políticas aplicables al contexto
    /// y retorna una decisión final.
    /// Nunca retorna null — si no hay políticas, retorna Decision.Deny por defecto seguro.
    /// </summary>
    Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default);
}