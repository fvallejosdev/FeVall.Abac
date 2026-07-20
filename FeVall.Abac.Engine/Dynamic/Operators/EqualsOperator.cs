// FeVall.Abac.Engine/Dynamic/Operators/EqualsOperator.cs
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Operators
{

    /// <summary>Operador "Equals". internal sealed: se registra en DI, nunca se instancia directo.</summary>
    internal sealed class EqualsOperator : IComparisonOperator
    {
        public string Name => "Equals";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            AttributeValueComparer.AreEqual(actualValue, expectedValue);
    }

    /// <summary>Operador "NotEquals" — negación directa de Equals.</summary>
    internal sealed class NotEqualsOperator : IComparisonOperator
    {
        public string Name => "NotEquals";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            !AttributeValueComparer.AreEqual(actualValue, expectedValue);
    }
}
