// FeVall.Abac.Engine/Strategies/DenyOverridesStrategy.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Engine.Strategies;

/// <summary>
/// Estrategia de combinación: cualquier Deny tiene prioridad sobre todos los Permit.
/// La más segura — usada cuando el acceso no autorizado es crítico.
/// OCP: nueva estrategia = nueva clase, PolicyEvaluator no se toca.
/// internal sealed: detalle de implementación del motor.
/// </summary>
internal sealed class DenyOverridesStrategy : ICombinationStrategy
{
    public string Name => nameof(DenyOverridesStrategy);

    /// <summary>
    /// Si al menos una decisión es Deny, el resultado final es Deny.
    /// Solo retorna Permit si todas las decisiones son Permit.
    /// </summary>
    public Decision Combine(IReadOnlyList<Decision> decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);

        var firstDeny = FindFirstDeny(decisions);

        return firstDeny is not null
            ? Decision.DenyWith(BuildDenyReason(firstDeny))
            : Decision.PermitWith("Todas las políticas retornaron Permit.");
    }

    // Clean Code: un nivel de abstracción por función.
    // Combine habla en términos de alto nivel — delega el detalle a métodos privados.
    private static Decision? FindFirstDeny(IReadOnlyList<Decision> decisions) =>
        decisions.FirstOrDefault(d => d.IsDeny);

    private static string BuildDenyReason(Decision deny) =>
        deny.Reason is { Length: > 0 } reason
            ? $"[{nameof(DenyOverridesStrategy)}] Denegado por política: {reason}"
            : $"[{nameof(DenyOverridesStrategy)}] Denegado sin razón especificada.";
}