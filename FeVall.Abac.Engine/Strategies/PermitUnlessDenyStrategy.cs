// FeVall.Abac.Engine/Strategies/PermitUnlessDenyStrategy.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Strategies;

/// <summary>
/// Estrategia de combinación: Permit a menos que haya un Deny explícito.
/// Más permisiva que DenyOverridesStrategy.
/// Usada cuando el acceso es la norma y se deniega solo por excepción explícita.
/// OCP: nueva estrategia = nueva clase, PolicyEvaluator no se toca.
/// Implementa IShortCircuitCombinationStrategy con un criterio MÁS ESTRICTO que
/// DenyOverridesStrategy: aquí solo un Deny CON razón explícita es decisivo —
/// coherente con Combine/FindExplicitDeny, que ignora los Deny sin razón.
/// Un Deny sin razón NO debe cortocircuitar, porque esta estrategia lo trata
/// como si no hubiera decidido nada.
/// internal sealed: detalle de implementación del motor.
/// </summary>
internal sealed class PermitUnlessDenyStrategy : IShortCircuitCombinationStrategy
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

    /// <summary>
    /// Solo un Deny CON razón es decisivo aquí — debe coincidir exactamente
    /// con el criterio de FindExplicitDeny en Combine, o el cortocircuito
    /// podría detenerse antes de tiempo (falso positivo) o después (Deny sin
    /// razón nunca sería decisivo de todas formas, así que no hay riesgo de
    /// cortar antes de tiempo, pero sí de inconsistencia si el criterio diverge).
    /// </summary>
    public bool IsDecisive(Decision decision) =>
       decision.IsDeny && decision.Reason is { Length: > 0 };

    private static Decision? FindExplicitDeny(IReadOnlyList<Decision> decisions) =>
        decisions.FirstOrDefault(d => d.IsDeny && d.Reason is { Length: > 0 });

    private static string BuildDenyReason(Decision deny) =>
        $"[{nameof(PermitUnlessDenyStrategy)}] Denegado explícitamente: {deny.Reason}";
}