// FeVall.Abac.Abstractions/EvaluationContextBuilder.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Construye un IEvaluationContext de forma legible y fluent.
/// SRP: la responsabilidad de construir el contexto no pertenece
/// ni a EvaluationContext ni al consumidor directo.
/// </summary>
public sealed class EvaluationContextBuilder
{
    private readonly EvaluationContext _context = new();

    private EvaluationContextBuilder() { }

    public static EvaluationContextBuilder Create() => new();

    public EvaluationContextBuilder WithSubject(Action<AttributeBag> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_context.Subject);
        return this;
    }

    public EvaluationContextBuilder WithResource(Action<AttributeBag> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_context.Resource);
        return this;
    }

    public EvaluationContextBuilder WithAction(Action<AttributeBag> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_context.Action);
        return this;
    }

    public EvaluationContextBuilder WithEnvironment(Action<AttributeBag> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_context.Environment);
        return this;
    }

    public IEvaluationContext Build() => _context;
}