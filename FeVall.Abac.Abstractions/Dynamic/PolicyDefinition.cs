// FeVall.Abac.Abstractions/Dynamic/PolicyDefinition.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Representación fiel del JSON de política creado por el usuario en la UI de administración.
    /// Es un DTO puro — no contiene lógica de evaluación (SRP). La lógica vive en IPolicyCompiler,
    /// que transforma esta definición "de datos" en un IPolicy "de comportamiento".
    /// </summary>
    public sealed record PolicyDefinition
    {
        public required string PolicyId { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }

        /// <summary>
        /// Condición de aplicabilidad (Target). Si es null, la política aplica siempre.
        /// Se mapea a IPolicyApplicability en tiempo de compilación.
        /// </summary>
        public ConditionDefinition? Target { get; init; }

        /// <summary>Condición de negocio que determina Permit/Deny.</summary>
        public required ConditionDefinition Rule { get; init; }

        public IReadOnlyList<ObligationDefinition> Obligations { get; init; } = [];
    }

    /// <summary>
    /// Nodo del árbol de condiciones. Puede ser una hoja (compara un atributo)
    /// o un nodo compuesto (agrupa sub-condiciones con And/Or/Not).
    /// La misma clase modela ambos casos porque así lo hace el JSON de origen;
    /// IsLeaf distingue el caso en tiempo de compilación.
    /// </summary>
    public sealed record ConditionDefinition
    {
        public string? Description { get; init; }

        // --- Caso hoja ---
        public string? Attribute { get; init; }

        /// <summary>
        /// Nombre del operador. En una hoja es un operador de comparación (Equals, In, ...).
        /// En un nodo compuesto es un operador lógico (And, Or, Not). Ambos se resuelven
        /// contra listas blancas distintas — nunca se ejecuta código arbitrario.
        /// </summary>
        public string? Operator { get; init; }

        public object? Value { get; init; }

        // --- Caso compuesto ---
        public IReadOnlyList<ConditionDefinition> Conditions { get; init; } = [];

        /// <summary>Una hoja se identifica por declarar un Attribute.</summary>
        public bool IsLeaf => Attribute is not null;
    }

    public sealed record ObligationDefinition
    {
        public required string Id { get; init; }

        /// <summary>"Permit" o "Deny" — cuándo se debe adjuntar esta obligación.</summary>
        public required string FulfillOn { get; init; }

        public IReadOnlyDictionary<string, object?> Parameters { get; init; } =
            new Dictionary<string, object?>();
    }
}
