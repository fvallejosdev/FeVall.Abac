// FeVall.Abac.Engine/Dynamic/ShortCircuitPolicyEvaluator.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Reemplazo de PolicyEvaluator que agrega dos capacidades sin romper SRP:
    ///  1) Lee políticas desde IPolicyProvider (permite mezclar políticas estáticas
    ///     registradas en DI con políticas dinámicas creadas por la UI).
    ///  2) Cortocircuita la evaluación: si la estrategia de combinación implementa
    ///     IShortCircuitCombinationStrategy y una decisión ya es definitiva
    ///     (ej. un Deny bajo DenyOverrides), deja de evaluar las políticas restantes.
    /// Con 10 políticas y DenyOverrides, si la política #1 deniega, las 9 restantes
    /// nunca se ejecutan — ahorro directo de CPU y, si alguna política hace I/O
    /// (consultas a BD para resolver atributos), de red también.
    /// internal sealed: se registra como IPolicyEvaluator vía DI, igual que PolicyEvaluator.
    /// </summary>
    internal sealed class ShortCircuitPolicyEvaluator : IPolicyEvaluator
    {
        private readonly IPolicyProvider _policyProvider;
        private readonly ICombinationStrategy _strategy;
        private readonly IAbacLogger _logger;

        public ShortCircuitPolicyEvaluator(
            IPolicyProvider policyProvider,
            ICombinationStrategy strategy,
            IAbacLogger logger)
        {
            _policyProvider = policyProvider;
            _strategy = strategy;
            _logger = logger;
        }

        public async Task<Decision> EvaluateAsync(IEvaluationContext context, CancellationToken ct = default)
        {
            var policies = await _policyProvider.GetPoliciesAsync(ct);
            var decisions = await CollectDecisionsAsync(policies, context, ct);

            return decisions.Count is 0
                ? Decision.DenyWith("No hay políticas aplicables al contexto.")
                : _strategy.Combine(decisions);
        }

        private async Task<List<Decision>> CollectDecisionsAsync(
            IReadOnlyList<IPolicy> policies,
            IEvaluationContext context,
            CancellationToken ct)
        {
            var decisions = new List<Decision>();

            foreach (var policy in policies)
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

                if (IsDecisive(decision))
                    break; // Cortocircuito: el resultado ya está determinado.
            }

            return decisions;
        }

        private bool IsDecisive(Decision decision) =>
            _strategy is IShortCircuitCombinationStrategy shortCircuit && shortCircuit.IsDecisive(decision);

        private static bool ShouldSkip(IPolicy policy, IEvaluationContext context) =>
            policy is IPolicyApplicability applicability && !applicability.IsApplicableTo(context);
    }
}
