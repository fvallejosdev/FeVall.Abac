// FeVall.Abac.Engine/Dynamic/ShortCircuitPolicyEvaluator.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Reemplazo de PolicyEvaluator que agrega dos capacidades sin romper SRP:
    ///  1) Combina políticas ESTÁTICAS (registradas en DI vía IEnumerable&lt;IPolicy&gt;,
    ///     el mismo mecanismo que usa PolicyEvaluator) con políticas DINÁMICAS
    ///     (leídas de IPolicyProvider, creadas por la UI de administración).
    ///     Las estáticas se resuelven una sola vez, en el constructor — no cambian
    ///     entre requests. Las dinámicas se consultan en cada EvaluateAsync porque
    ///     sí pueden cambiar sin reiniciar el proceso (ver DynamicPolicyCache).
    ///  2) Cortocircuita la evaluación: si la estrategia de combinación implementa
    ///     IShortCircuitCombinationStrategy y una decisión ya es definitiva
    ///     (ej. un Deny bajo DenyOverrides), deja de evaluar las políticas restantes.
    /// Las políticas estáticas se evalúan primero — se asume que fueron revisadas
    /// en code review (mismo criterio documentado en FaultTolerantPolicyDecorator,
    /// que solo envuelve políticas dinámicas) y suelen ser las reglas de negocio
    /// más "duras"; evaluarlas antes maximiza el beneficio del cortocircuito.
    /// internal sealed: se registra como IPolicyEvaluator vía DI, igual que PolicyEvaluator.
    /// </summary>
    internal sealed class ShortCircuitPolicyEvaluator : IPolicyEvaluator
    {
        private readonly IReadOnlyList<IPolicy> _staticPolicies;
        private readonly IPolicyProvider _policyProvider;
        private readonly ICombinationStrategy _strategy;
        private readonly IAbacLogger _logger;

        public ShortCircuitPolicyEvaluator(
           IEnumerable<IPolicy> staticPolicies,
           IPolicyProvider policyProvider,
           ICombinationStrategy strategy,
           IAbacLogger logger)
        {
            ArgumentNullException.ThrowIfNull(staticPolicies);
            ArgumentNullException.ThrowIfNull(policyProvider);
            ArgumentNullException.ThrowIfNull(strategy);
            ArgumentNullException.ThrowIfNull(logger);

            // Materializado una vez: IEnumerable<IPolicy> de DI es barato de recorrer,
            // pero no queremos volver a enumerarlo (y potencialmente re-resolver
            // instancias Scoped/Transient) en cada EvaluateAsync.
            _staticPolicies = staticPolicies.ToArray();
            _policyProvider = policyProvider;
            _strategy = strategy;
            _logger = logger;
        }

        public async Task<Decision> EvaluateAsync(IEvaluationContext context, CancellationToken ct = default)
        {
            var dynamicPolicies = await _policyProvider.GetPoliciesAsync(ct);
            var allPolicies = CombinePolicies(_staticPolicies, dynamicPolicies);

            var decisions = await CollectDecisionsAsync(allPolicies, context, ct);

            return decisions.Count is 0
                ? Decision.DenyWith("No hay políticas aplicables al contexto.")
                : _strategy.Combine(decisions);
        }

        private static IReadOnlyList<IPolicy> CombinePolicies(
            IReadOnlyList<IPolicy> staticPolicies,
            IReadOnlyList<IPolicy> dynamicPolicies)
        {
            if (staticPolicies.Count == 0) return dynamicPolicies;
            if (dynamicPolicies.Count == 0) return staticPolicies;

            // IReadOnlyList<T> no expone CopyTo (es de ICollection<T>/Array) —
            // se copia por índice, sin LINQ, para mantener esto en el hot path
            // sin asignaciones intermedias de iteradores.
            var combined = new IPolicy[staticPolicies.Count + dynamicPolicies.Count];

            for (var i = 0; i < staticPolicies.Count; i++)
                combined[i] = staticPolicies[i];

            for (var i = 0; i < dynamicPolicies.Count; i++)
                combined[staticPolicies.Count + i] = dynamicPolicies[i];

            return combined;
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
