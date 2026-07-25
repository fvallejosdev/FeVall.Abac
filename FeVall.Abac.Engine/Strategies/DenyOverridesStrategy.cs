// FeVall.Abac.Engine/Strategies/DenyOverridesStrategy.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Strategies;

/// <summary>
/// Estrategia de combinación: cualquier Deny tiene prioridad sobre todos los Permit.
/// La más segura — usada cuando el acceso no autorizado es crítico.
/// OCP: nueva estrategia = nueva clase, PolicyEvaluator no se toca.
/// Implementa IShortCircuitCombinationStrategy: bajo DenyOverrides, el PRIMER
/// Deny que aparece ya determina el resultado final matemáticamente — evaluar
/// las políticas restantes es trabajo desperdiciado. Esto es lo que permite a
/// ShortCircuitPolicyEvaluator dejar de evaluar en cuanto encuentra un Deny.
/// internal sealed: detalle de implementación del motor.
/// </summary>
internal sealed class DenyOverridesStrategy : IShortCircuitCombinationStrategy
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

    /// <summary>
    /// Un Deny, sea cual sea su razón, ya es decisivo bajo DenyOverrides:
    /// ninguna política restante puede cambiar el resultado final a Permit.
    /// </summary>
    public bool IsDecisive(Decision decision) => decision.IsDeny;

    private static Decision? FindFirstDeny(IReadOnlyList<Decision> decisions) =>
        decisions.FirstOrDefault(d => d.IsDeny);

    private static string BuildDenyReason(Decision deny) =>
       deny.Reason is { Length: > 0 } reason
           ? $"[{nameof(DenyOverridesStrategy)}] Denegado por política: {reason}"
           : $"[{nameof(DenyOverridesStrategy)}] Denegado sin razón especificada.";
}