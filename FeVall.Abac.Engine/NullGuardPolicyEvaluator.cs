// FeVall.Abac.Engine/NullGuardPolicyEvaluator.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Engine;

/// <summary>
/// Decorator de IPolicyEvaluator que valida el contexto antes de evaluar.
/// OCP: agrega validación sin modificar PolicyEvaluator.
/// SRP: su única responsabilidad es garantizar que el contexto es válido
///      antes de pasarlo al evaluador real.
/// internal sealed: detalle de implementación del motor.
/// </summary>
internal sealed class NullGuardPolicyEvaluator : IPolicyEvaluator
{
    private readonly IPolicyEvaluator _inner;

    public NullGuardPolicyEvaluator(IPolicyEvaluator inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    public Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ValidateContext(context);

        return _inner.EvaluateAsync(context, ct);
    }

    // Clean Code: validación separada en método con nombre claro.
    // Cada guard tiene su propio mensaje — el llamador sabe exactamente qué faltó.
    private static void ValidateContext(IEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Subject,
            $"{nameof(context.Subject)} no puede ser null.");

        ArgumentNullException.ThrowIfNull(context.Resource,
            $"{nameof(context.Resource)} no puede ser null.");

        ArgumentNullException.ThrowIfNull(context.Action,
            $"{nameof(context.Action)} no puede ser null.");

        ArgumentNullException.ThrowIfNull(context.Environment,
            $"{nameof(context.Environment)} no puede ser null.");
    }
}