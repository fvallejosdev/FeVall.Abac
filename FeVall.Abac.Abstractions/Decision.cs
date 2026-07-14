namespace FeVall.Abac.Abstractions;

/// <summary>
/// Resultado inmutable de una evaluación ABAC.
/// </summary>
public sealed record Decision
{
    public static readonly Decision Permit = new(DecisionEffect.Permit);
    public static readonly Decision Deny = new(DecisionEffect.Deny);

    public DecisionEffect Effect { get; }
    public string? Reason { get; init; }
    public IReadOnlyList<string> Obligations { get; init; } = [];

    public bool IsPermit => Effect is DecisionEffect.Permit;
    public bool IsDeny => Effect is DecisionEffect.Deny;

    private Decision(DecisionEffect effect) => Effect = effect;

    /// <summary>
    /// Permit con contexto adicional.
    /// </summary>
    public static Decision PermitWith(string reason) =>
        Permit with { Reason = reason };

    /// <summary>
    /// Deny con razón obligatoria — siempre se debe saber por qué se denegó.
    /// </summary>
    public static Decision DenyWith(string reason) =>
        Deny with { Reason = reason };
}

public enum DecisionEffect
{
    Permit,
    Deny
}