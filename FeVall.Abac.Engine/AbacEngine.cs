// FeVall.Abac.Engine/AbacEngine.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Engine;

/// <summary>
/// Orquestador principal del motor ABAC.
/// SRP: su única responsabilidad es coordinar el flujo — no evalúa, no combina, no valida.
/// internal sealed: el consumidor nunca instancia esta clase directamente.
///                  La única forma de obtenerla es a través de IAbacEngine vía DI.
/// </summary>
internal sealed class AbacEngine : IAbacEngine
{
    private readonly IPolicyEvaluator _evaluator;
    private readonly IAbacLogger _logger;

    public AbacEngine(IPolicyEvaluator evaluator, IAbacLogger logger)
    {
        _evaluator = evaluator;
        _logger = logger;
    }

    public async Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default)
    {
        _logger.LogEvaluationStarted(context);

        var decision = await _evaluator.EvaluateAsync(context, ct);

        _logger.LogDecisionReached(context, decision);

        return decision;
    }
}