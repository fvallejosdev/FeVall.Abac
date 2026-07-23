
// FeVall.Abac.Abstractions/Dynamic/ConditionTrace.cs
namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Árbol de diagnóstico que refleja 1:1 la estructura del árbol de condiciones
    /// compilado, pero con el resultado y los valores reales de cada nodo.
    /// Es lo que la UI de administración renderiza para que un usuario no-técnico
    /// entienda por qué su política denegó (o permitió) un caso concreto.
    /// DTO puro — sin lógica, igual que PolicyDefinition (SRP).
    /// </summary>
    public sealed record ConditionTrace
    {
        public required bool IsSatisfied { get; init; }
        public string? Description { get; init; }

        // Presentes solo en nodos hoja (comparación de atributo)
        public string? Attribute { get; init; }
        public string? Operator { get; init; }
        public object? ActualValue { get; init; }
        public object? ExpectedValue { get; init; }

        // Presente solo en nodos compuestos (And/Or/Not)
        public string? LogicalOperator { get; init; }
        public IReadOnlyList<ConditionTrace> Children { get; init; } = [];
    }
}
