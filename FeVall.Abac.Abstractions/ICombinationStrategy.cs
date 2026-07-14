// FeVall.Abac.Abstractions/ICombinationStrategy.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Define cómo se combinan las decisiones individuales de múltiples políticas
/// en una decisión final única.
/// OCP: agregar una nueva estrategia de combinación significa crear una nueva clase,
///      nunca modificar el evaluador ni el motor.
/// </summary>
public interface ICombinationStrategy
{
    /// <summary>
    /// Nombre de la estrategia. Usado en logging y configuración.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Combina una colección de decisiones en una decisión final.
    /// Nunca recibe una colección vacía — el evaluador lo garantiza antes de llamar.
    /// </summary>
    Decision Combine(IReadOnlyList<Decision> decisions);
}