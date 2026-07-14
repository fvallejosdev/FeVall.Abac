// FeVall.Abac.Engine/Extensions/AbacEngineOptions.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Engine.Extensions;

/// <summary>
/// Opciones de configuración del motor ABAC.
/// SRP: centraliza toda la configuración en un solo lugar.
/// El consumidor las ajusta al registrar el motor en DI.
/// </summary>
public sealed class AbacEngineOptions
{
    /// <summary>
    /// Decisión por defecto cuando no hay políticas aplicables al contexto.
    /// Default: Deny — principio de mínimo privilegio.
    /// </summary>
    public Decision DefaultDecision { get; set; } = Decision.Deny;

    /// <summary>
    /// Estrategia de combinación a usar.
    /// Default: DenyOverrides — la más segura para sistemas de control de acceso.
    /// </summary>
    public CombinationStrategy CombinationStrategy { get; set; } =
        CombinationStrategy.DenyOverrides;

    /// <summary>
    /// Si es true, una excepción no controlada dentro de una política
    /// retorna Deny en lugar de propagar la excepción.
    /// Default: true — el motor nunca explota por una política mal implementada.
    /// </summary>
    public bool DenyOnPolicyException { get; set; } = true;

    /// <summary>
    /// Si es true, el motor registra en Debug el detalle de cada política evaluada.
    /// Default: false — evita ruido en producción.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// Estrategias de combinación disponibles en el motor.
/// Enum cerrado — el consumidor elige entre las estrategias incluidas.
/// Para estrategias personalizadas, implementar ICombinationStrategy directamente.
/// </summary>
public enum CombinationStrategy
{
    /// <summary>Cualquier Deny tiene prioridad sobre todos los Permit.</summary>
    DenyOverrides,

    /// <summary>Permit a menos que haya un Deny explícito con razón.</summary>
    PermitUnlessDeny
}