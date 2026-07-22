// FeVall.Abac.Abstractions/Obligation.cs

namespace FeVall.Abac.Abstractions
{
    /// <summary>
    /// Obligación adjunta a una Decision, con sus parámetros ya normalizados a tipos
    /// CLR simples (no JsonElement crudo) — la normalización ocurre en JsonPolicyCompiler,
    /// en tiempo de COMPILACIÓN, nunca en EvaluateAsync (mismo principio que
    /// JsonValueNormalizer aplica a los Value de las condiciones).
    /// </summary>
    public sealed record Obligation
    {
        public required string Id { get; init; }
        public IReadOnlyDictionary<string, object?> Parameters { get; init; } =
            new Dictionary<string, object?>();
    }
}
