// FeVall.Abac.Abstractions/EvaluationContext.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Implementación estándar de IEvaluationContext.
/// Construida vía EvaluationContextBuilder — nunca directamente.
/// </summary>
public sealed class EvaluationContext : IEvaluationContext
{
    public AttributeBag Subject { get; } = new();
    public AttributeBag Resource { get; } = new();
    public AttributeBag Action { get; } = new();
    public AttributeBag Environment { get; } = new();
}