// FeVall.Abac.Engine/Dynamic/CompiledPolicy.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// IPolicy producido por JsonPolicyCompiler a partir de una PolicyDefinition.
    /// Implementa IPolicyApplicability usando el árbol "Target" — el PolicyEvaluator
    /// existente ya sabe cómo tratar esto (ShouldSkip), sin ningún cambio adicional.
    /// internal sealed: solo se construye desde JsonPolicyCompiler.
    /// </summary>
    internal sealed class CompiledPolicy : IPolicy, IPolicyApplicability
    {
        private readonly IConditionNode? _target;
        private readonly IConditionNode _rule;
        private readonly IReadOnlyList<Obligation> _permitObligations;
        private readonly IReadOnlyList<Obligation> _denyObligations;
        private readonly bool _explainOnDeny;

        public string Name { get; }

        internal CompiledPolicy(
         string name,
         IConditionNode? target,
         IConditionNode rule,
         IReadOnlyList<Obligation> permitObligations,
         IReadOnlyList<Obligation> denyObligations,
         bool explainOnDeny = true)
        {
            Name = name;
            _target = target;
            _rule = rule;
            _permitObligations = permitObligations;
            _denyObligations = denyObligations;
            _explainOnDeny = explainOnDeny;
        }
        public bool IsApplicableTo(IEvaluationContext context) => _target?.IsSatisfiedBy(context) ?? true;
        public Task<Decision> EvaluateAsync(IEvaluationContext context, CancellationToken ct = default)
        {
            var isPermit = _rule.IsSatisfiedBy(context);

            var decision = isPermit
                ? Decision.PermitWith($"Política '{Name}': todas las condiciones de la regla se cumplieron.")
                : BuildDenyDecision(context);

            // Ya no hay Where/Select aquí — solo elegir cuál lista precalculada usar.
            var obligations = isPermit ? _permitObligations : _denyObligations;

            return Task.FromResult(
                obligations.Count == 0 ? decision : decision with { Obligations = obligations });
        }

        private Decision BuildDenyDecision(IEvaluationContext context)
        {
            if (!_explainOnDeny || _rule is not IExplainableConditionNode explainable)
                return Decision.DenyWith($"Política '{Name}': la regla no se cumplió.");

            var trace = explainable.Explain(context);
            var failingLeaves = CollectFailingLeaves(trace);

            var reason = failingLeaves.Count == 0
                ? $"Política '{Name}': la regla no se cumplió."
                : $"Política '{Name}': falló → {string.Join("; ", failingLeaves)}";

            return Decision.DenyWith(reason) with { Trace = trace };
        }

        // Aplana el árbol a las hojas específicas que fallaron — es lo que un
        // humano quiere leer primero, no el árbol lógico completo.
        private static List<string> CollectFailingLeaves(ConditionTrace trace)
        {
            var failures = new List<string>();
            Walk(trace);
            return failures;

            void Walk(ConditionTrace node)
            {
                if (node.IsSatisfied) return;

                if (node.Attribute is not null)
                {
                    var label = node.Description ?? node.Attribute;
                    failures.Add($"{label} ({node.Attribute} {node.Operator} {node.ExpectedValue}, valor real: {node.ActualValue ?? "ausente"})");
                    return;
                }

                foreach (var child in node.Children)
                    Walk(child);
            }
        }

    }
}
