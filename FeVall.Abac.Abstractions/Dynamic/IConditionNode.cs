// FeVall.Abac.Abstractions/Dynamic/IConditionNode.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Nodo evaluable de un árbol de condiciones (patrón Composite).
    /// Tanto una comparación de atributo (hoja) como un And/Or/Not (rama)
    /// implementan esta misma interfaz — el evaluador nunca distingue el caso.
    /// LSP: cualquier IConditionNode puede sustituir a otro sin romper el árbol.
    /// Deliberadamente síncrona: los atributos ya deben estar resueltos en el
    /// IEvaluationContext antes de evaluar (ver IAttributeBatchLoader para evitar N+1).
    /// </summary>
    public interface IConditionNode
    {
        bool IsSatisfiedBy(IEvaluationContext context);
    }
}
