// FeVall.Abac.Engine/Dynamic/Operators/InOperator.cs
using System.Collections;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Operators
{

    /// <summary>Operador "In" — el valor esperado es una lista literal (ej. ["Read","Modify"]).</summary>
    internal sealed class InOperator : IComparisonOperator
    {
        public string Name => "In";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            AsEnumerable(expectedValue).Any(candidate => AttributeValueComparer.AreEqual(actualValue, candidate));

        internal static IEnumerable<object?> AsEnumerable(object? value) => value switch
        {
            null => [],
            IEnumerable enumerable and not string => enumerable.Cast<object?>(),
            _ => [value]
        };
    }

    internal sealed class NotInOperator : IComparisonOperator
    {
        public string Name => "NotIn";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            !InOperator.AsEnumerable(expectedValue).Any(candidate => AttributeValueComparer.AreEqual(actualValue, candidate));
    }
}