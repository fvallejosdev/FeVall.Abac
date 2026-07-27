// FeVall.Abac.Engine/Dynamic/Operators/BetweenOperator.cs
using System.Collections;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Operators
{
    /// <summary>
    /// Operador "Between": el Value esperado es un array literal de exactamente
    /// dos elementos [min, max] (inclusive en ambos extremos).
    /// Implementa IValueValidatingOperator: la forma del Value (2 elementos,
    /// numéricos, min &lt;= max) se valida en Compile(), no en cada Evaluate() —
    /// mismo criterio fail-fast que el resto de la validación de forma en
    /// JsonPolicyCompiler (Value ausente, operador lógico en hoja, etc.).
    /// </summary>
    internal sealed class BetweenOperator : IValueValidatingOperator
    {
        public string Name => "Between";
        public bool ValueIsAttributeReference => false;

        public void ValidateValueShape(object? normalizedValue) => ExtractBounds(normalizedValue);

        public bool Evaluate(object? actualValue, object? expectedValue)
        {
            var (min, max) = ExtractBounds(expectedValue);
            var actual = GreaterThanOperator.ToDouble(actualValue);
            return actual >= min && actual <= max;
        }

        internal static (double Min, double Max) ExtractBounds(object? expectedValue)
        {
            if (expectedValue is null or string || expectedValue is not IEnumerable enumerable)
                throw new InvalidOperationException(
                    "El operador 'Between' requiere un Value con exactamente dos elementos [min, max].");

            var values = enumerable.Cast<object?>().ToList();
            if (values.Count != 2)
                throw new InvalidOperationException(
                    $"El operador 'Between' requiere exactamente 2 valores [min, max], se recibieron {values.Count}.");

            var min = GreaterThanOperator.ToDouble(values[0]);
            var max = GreaterThanOperator.ToDouble(values[1]);

            if (min > max)
                throw new InvalidOperationException(
                    $"El rango 'Between' es inválido: min ({min}) es mayor que max ({max}).");

            return (min, max);
        }
    }

    internal sealed class NotBetweenOperator : IValueValidatingOperator
    {
        public string Name => "NotBetween";
        public bool ValueIsAttributeReference => false;

        public void ValidateValueShape(object? normalizedValue) => BetweenOperator.ExtractBounds(normalizedValue);

        public bool Evaluate(object? actualValue, object? expectedValue)
        {
            var (min, max) = BetweenOperator.ExtractBounds(expectedValue);
            var actual = GreaterThanOperator.ToDouble(actualValue);
            return actual < min || actual > max;
        }
    }

}
