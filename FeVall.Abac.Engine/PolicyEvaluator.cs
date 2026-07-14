// FeVall.Abac.Engine/PolicyEvaluator.cs
using FeVall.Abac.Abstractions;

namespace FeVall.Abac.Engine;

/// <summary>
/// Evalúa todas las políticas registradas contra un contexto dado.
/// SRP: coordina la evaluación — no combina resultados ni valida el contexto.
/// OCP: nuevas políticas se agregan registrándolas en DI, sin tocar esta clase.
/// internal sealed: detalle de implementación del motor.
/// </summary>
internal sealed class PolicyEvaluator : IPolicyEvaluator
{
    private readonly IEnumerable<IPolicy> _policies;
    private readonly ICombinationStrategy _strategy;
    private readonly IAbacLogger _logger;

    public PolicyEvaluator(
        IEnumerable<IPolicy> policies,
        ICombinationStrategy strategy,
        IAbacLogger logger)
    {
        _policies = policies;
        _strategy = strategy;
        _logger = logger;
    }

    public async Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default)
    {
        var decisions = await CollectDecisionsAsync(context, ct);

        return decisions.Count is 0
            ? Decision.DenyWith("No hay políticas aplicables al contexto.")
            : _strategy.Combine(decisions);
    }

    // Clean Code: función privada con nombre que revela intención.
    // Un nivel de abstracción por función — EvaluateAsync no mezcla
    // iteración con combinación.
    private async Task<List<Decision>> CollectDecisionsAsync(
        IEvaluationContext context,
        CancellationToken ct)
    {
        var decisions = new List<Decision>();

        foreach (var policy in _policies)
        {
            ct.ThrowIfCancellationRequested();

            if (ShouldSkip(policy, context))
            {
                _logger.LogPolicySkipped(policy, context);
                continue;
            }

            var decision = await policy.EvaluateAsync(context, ct);
            _logger.LogPolicyEvaluated(policy, decision);
            decisions.Add(decision);
        }

        return decisions;
    }

    // Clean Code: nombre explícito — evita comentarios innecesarios.
    // ISP: solo consulta IPolicyApplicability si la política la implementa.
    private static bool ShouldSkip(IPolicy policy, IEvaluationContext context) =>
        policy is IPolicyApplicability applicability &&
        !applicability.IsApplicableTo(context);
}