// FeVall.Abac.Engine/Dynamic/Operators/ComparisonOperators.cs
using System.Globalization;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Operators
{

    /// <summary>
    /// Operadores de orden numérico. Deliberadamente NO capturan excepciones de conversión:
    /// si un atributo no es convertible a double, la excepción sube hasta
    /// FaultTolerantPolicyDecorator, que la traduce en Deny (fail-closed centralizado,
    /// en vez de que cada operador decida su propio comportamiento ante datos corruptos).
    /// </summary>
    internal sealed class GreaterThanOperator : IComparisonOperator
    {
        public string Name => "GreaterThan";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            ToDouble(actualValue) > ToDouble(expectedValue);
        internal static double ToDouble(object? value) => value switch
        {
            null => throw new InvalidOperationException("Atributo numérico ausente o nulo."),
            _ => Convert.ToDouble(value, CultureInfo.InvariantCulture)
        };
    }

    internal sealed class LessThanOperator : IComparisonOperator
    {
        public string Name => "LessThan";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            GreaterThanOperator.ToDouble(actualValue) < GreaterThanOperator.ToDouble(expectedValue);
    }
}