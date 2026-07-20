// FeVall.Abac.Engine/Dynamic/Operators/AttributeValueComparer.cs
namespace FeVall.Abac.Engine.Dynamic.Operators
{

    /// <summary>
    /// Compara dos valores de atributo tolerando que provengan de fuentes distintas
    /// (el contexto suele tener tipos .NET fuertes — bool, int — mientras que el JSON
    /// de la política, ya normalizado, suele traer string/double/bool).
    /// SRP: toda la heurística de "¿son iguales?" vive en un solo lugar,
    /// reutilizada por EqualsOperator, InOperator y NotInOperator.
    /// </summary>
    internal static class AttributeValueComparer
    {
        public static bool AreEqual(object? left, object? right)
        {
            if (left is null || right is null)
                return left is null && right is null;

            if (left.GetType() == right.GetType())
                return left.Equals(right);

            // Tipos distintos: normalizamos a texto (invariant, sin distinguir mayúsculas)
            // en lugar de intentar conversiones numéricas arriesgadas para Equals/In.
            return string.Equals(
                Convert.ToString(left, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(right, System.Globalization.CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
