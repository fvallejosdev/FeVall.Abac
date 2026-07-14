// FeVall.Abac.Engine/Strategies/PermitUnlessDenyStrategy.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Engine.Strategies;

/// <summary>
/// Estrategia de combinación: Permit a menos que haya un Deny explícito.
/// Más permisiva que DenyOverridesStrategy.
/// Usada cuando el acceso es la norma y se deniega solo por excepción explícita.
/// OCP: nueva estrategia = nueva clase, PolicyEvaluator no se toca.
/// internal sealed: detalle de implementación del motor.
/// </summary>
internal sealed class PermitUnlessDenyStrategy : ICombinationStrategy
{
    public string Name => nameof(PermitUnlessDenyStrategy);

    /// <summary>
    /// Retorna Permit salvo que exista al menos un Deny explícito.
    /// A diferencia de DenyOverridesStrategy, un Deny sin razón
    /// no es suficiente — debe ser un Deny con razón explícita.
    /// </summary>
    public Decision Combine(IReadOnlyList<Decision> decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);

        var explicitDeny = FindExplicitDeny(decisions);

        return explicitDeny is not null
            ? Decision.DenyWith(BuildDenyReason(explicitDeny))
            : Decision.PermitWith("Ninguna política denegó explícitamente.");
    }

    // Un Deny "explícito" es aquel que tiene una razón declarada.
    // Un Deny sin razón se trata como ausencia de decisión en esta estrategia.
    private static Decision? FindExplicitDeny(IReadOnlyList<Decision> decisions) =>
        decisions.FirstOrDefault(d => d.IsDeny && d.Reason is { Length: > 0 });

    private static string BuildDenyReason(Decision deny) =>
        $"[{nameof(PermitUnlessDenyStrategy)}] Denegado explícitamente: {deny.Reason}";
}