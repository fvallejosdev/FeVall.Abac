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
        private readonly IReadOnlyList<ObligationDefinition> _obligations;

        public string Name { get; }

        internal CompiledPolicy(
        string name,
        IConditionNode? target,
        IConditionNode rule,
        IReadOnlyList<ObligationDefinition> obligations)
        {
            Name = name;
            _target = target;
            _rule = rule;
            _obligations = obligations;
        }

        public bool IsApplicableTo(IEvaluationContext context) =>
        _target?.IsSatisfiedBy(context) ?? true;

        public Task<Decision> EvaluateAsync(IEvaluationContext context, CancellationToken ct = default)
        {
            var isPermit = _rule.IsSatisfiedBy(context);

            var decision = isPermit
                ? Decision.PermitWith($"Política '{Name}': todas las condiciones de la regla se cumplieron.")
                : Decision.DenyWith($"Política '{Name}': la regla no se cumplió.");

            var obligationIds = CollectObligations(isPermit);

            return Task.FromResult(
                obligationIds.Count == 0 ? decision : decision with { Obligations = obligationIds });
        }

        private IReadOnlyList<string> CollectObligations(bool isPermit)
        {
            var expectedFulfillOn = isPermit ? "Permit" : "Deny";

            return _obligations
                .Where(o => string.Equals(o.FulfillOn, expectedFulfillOn, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.Id)
                .ToArray();
        }

    }
}
