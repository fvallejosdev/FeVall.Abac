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
    internal sealed class AttributeConditionNode : IConditionNode
    {
        private readonly string _attributePath;
        private readonly IComparisonOperator _operator;
        private readonly object? _expectedValue;

        public AttributeConditionNode(string attributePath, IComparisonOperator @operator, object? expectedValue)
        {
            _attributePath = attributePath;
            _operator = @operator;
            _expectedValue = expectedValue;
        }

        public bool IsSatisfiedBy(IEvaluationContext context)
        {
            var actual = AttributePathResolver.Resolve(context, _attributePath);

            var expected = _operator.ValueIsAttributeReference
                ? AttributePathResolver.Resolve(context, (string)_expectedValue!)
                : _expectedValue;

            return _operator.Evaluate(actual, expected);
        }
    }
}