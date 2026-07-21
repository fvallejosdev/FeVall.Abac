// FeVall.Abac.Engine/Dynamic/Operators/TextOperators.cs
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Operators
{
    /// <summary>
    /// Utilidad compartida de conversión a texto para operadores de substring.
    /// Fail-closed: un valor ausente lanza en vez de tratarse como "" —
    /// mismo criterio que GreaterThanOperator.ToDouble para valores numéricos.
    /// </summary>
    /// 
    internal static class TextConverter
    {
        public static string ToText(object? value) => value switch
        {
            null => throw new InvalidOperationException("Atributo de texto ausente o nulo."),
            string s => s,
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                 ?? throw new InvalidOperationException("El atributo no pudo convertirse a texto.")
        };
    }
    internal sealed class StartsWithOperator : IComparisonOperator
    {
        public string Name => "StartsWith";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            TextConverter.ToText(actualValue)
                .StartsWith(TextConverter.ToText(expectedValue), StringComparison.OrdinalIgnoreCase);
    }
    internal sealed class EndsWithOperator : IComparisonOperator
    {
        public string Name => "EndsWith";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            TextConverter.ToText(actualValue)
                .EndsWith(TextConverter.ToText(expectedValue), StringComparison.OrdinalIgnoreCase);
    }
    internal sealed class ContainsTextOperator : IComparisonOperator
    {
        // "Contains" a secas ya podría confundirse con ContainsAttribute — nombre explícito en el JSON.
        public string Name => "ContainsText";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            TextConverter.ToText(actualValue)
                .Contains(TextConverter.ToText(expectedValue), StringComparison.OrdinalIgnoreCase);
    }
}
