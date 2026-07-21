// FeVall.Abac.Engine/Dynamic/Operators/DateTimeOperators.cs
using System.Globalization;
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Operators
{
    /// <summary>
    /// Conversión centralizada de atributos de fecha/hora. Acepta ISO 8601
    /// (el formato que JsonValueNormalizer produce para strings) y DateTimeOffset
    /// nativo si el AttributeBag ya lo tiene tipado así.
    /// Fail-closed: cualquier formato no parseable lanza — nunca se interpreta
    /// silenciosamente como "fecha mínima" o similar.
    /// </summary>
    /// 
    internal static class DateTimeConverter
    {
        public static DateTimeOffset ToDateTimeOffset(object? value) => value switch
        {
            null => throw new InvalidOperationException("Atributo de fecha ausente o nulo."),
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt),
            string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsed) => parsed,
            _ => throw new InvalidOperationException(
                     $"No se pudo interpretar '{value}' como fecha/hora (se espera ISO 8601).")
        };
    }

    /// <summary>"DateAfter": el atributo real ocurre DESPUÉS del valor esperado.</summary>
    internal sealed class DateAfterOperator : IComparisonOperator
    {
        public string Name => "DateAfter";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            DateTimeConverter.ToDateTimeOffset(actualValue) > DateTimeConverter.ToDateTimeOffset(expectedValue);
    }

    /// <summary>"DateBefore": el atributo real ocurre ANTES del valor esperado.</summary>
    internal sealed class DateBeforeOperator : IComparisonOperator
    {
        public string Name => "DateBefore";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            DateTimeConverter.ToDateTimeOffset(actualValue) < DateTimeConverter.ToDateTimeOffset(expectedValue);
    }

    /// <summary>
    /// "DateBetween": Value es [desde, hasta] en ISO 8601, ambos inclusive.
    /// Ej.: { "Attribute": "Environment.RequestTime", "Operator": "DateBetween",
    ///        "Value": ["2026-01-01T00:00:00Z", "2026-12-31T23:59:59Z"] }
    /// </summary>
    /// 
    internal sealed class DateBetweenOperator : IComparisonOperator
    {
        public string Name => "DateBetween";
        public bool ValueIsAttributeReference => false;

        public bool Evaluate(object? actualValue, object? expectedValue)
        {
            var (from, to) = ExtractRange(expectedValue);
            var actual = DateTimeConverter.ToDateTimeOffset(actualValue);
            return actual >= from && actual <= to;
        }

        private static (DateTimeOffset From, DateTimeOffset To) ExtractRange(object? expectedValue)
        {
            if (expectedValue is null or string || expectedValue is not System.Collections.IEnumerable enumerable)
                throw new InvalidOperationException(
                    "El operador 'DateBetween' requiere un Value con exactamente dos fechas [desde, hasta].");

            var values = enumerable.Cast<object?>().ToList();
            if (values.Count != 2)
                throw new InvalidOperationException(
                    $"El operador 'DateBetween' requiere exactamente 2 fechas, se recibieron {values.Count}.");

            var from = DateTimeConverter.ToDateTimeOffset(values[0]);
            var to = DateTimeConverter.ToDateTimeOffset(values[1]);

            if (from > to)
                throw new InvalidOperationException(
                    $"El rango 'DateBetween' es inválido: desde ({from:O}) es posterior a hasta ({to:O}).");

            return (from, to);
        }
    }
}
