// FeVall.Abac.Abstractions/Dynamic/IShortCircuitCombinationStrategy.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Extensión opcional de ICombinationStrategy que le permite al evaluador
    /// detener la evaluación de políticas restantes en cuanto el resultado
    /// ya es matemáticamente definitivo (ej. un Deny bajo DenyOverrides).
    /// ISP: separada de ICombinationStrategy para no obligar a todas las estrategias
    /// a razonar sobre cortocircuito — el mismo patrón que IPolicyApplicability
    /// usa frente a IPolicy en este codebase.
    /// </summary>
    public interface IShortCircuitCombinationStrategy : ICombinationStrategy
    {
        /// <summary>
        /// True si esta única decisión, por sí sola, ya determina el resultado final
        /// sin importar qué retornen las políticas restantes.
        /// </summary>
        bool IsDecisive(Decision decision);
    }
}
