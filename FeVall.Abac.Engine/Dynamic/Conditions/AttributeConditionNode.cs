// FeVall.Abac.Engine/Dynamic/Conditions/AttributeConditionNode.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;
using FeVall.Abac.Engine.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Conditions
{

    /// <summary>
    /// Hoja del árbol de condiciones: resuelve un atributo del contexto y lo compara
    /// contra un valor esperado usando un IComparisonOperator ya resuelto en tiempo de compilación.
    /// internal sealed: solo JsonPolicyCompiler construye instancias de esto.
    /// </summary>
    internal sealed class AttributeConditionNode : IExplainableConditionNode
    {
        private readonly string _attributePath;
        private readonly IComparisonOperator _operator;
        private readonly object? _expectedValue;
        private readonly string? _description;   // ← nuevo: viene de ConditionDefinition.Description

        public AttributeConditionNode(
        string attributePath,
        IComparisonOperator @operator,
        object? expectedValue,
        string? description = null)
        {
            _attributePath = attributePath;
            _operator = @operator;
            _expectedValue = expectedValue;
            _description = description;
        }

        public bool IsSatisfiedBy(IEvaluationContext context)
        {
            var actual = AttributePathResolver.Resolve(context, _attributePath);
            var expected = ResolveExpected(context);
            return _operator.Evaluate(actual, expected);
        }

        public ConditionTrace Explain(IEvaluationContext context)
        {
            var actual = AttributePathResolver.Resolve(context, _attributePath);
            var expected = ResolveExpected(context);
            var isSatisfied = _operator.Evaluate(actual, expected);

            return new ConditionTrace
            {
                IsSatisfied = isSatisfied,
                Description = _description,
                Attribute = _attributePath,
                Operator = _operator.Name,
                ActualValue = actual,
                ExpectedValue = expected
            };
        }

        private object? ResolveExpected(IEvaluationContext context) =>
         _operator.ValueIsAttributeReference
             ? AttributePathResolver.Resolve(context, (string)_expectedValue!)
             : _expectedValue;
    }
}