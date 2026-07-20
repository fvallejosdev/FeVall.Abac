// FeVall.Abac.Engine/Dynamic/JsonValueNormalizer.cs
using System.Text.Json;

namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Convierte los JsonElement crudos (que System.Text.Json produce al deserializar
    /// "object?") en tipos CLR simples (string, double, bool, List&lt;object?&gt;, null).
    /// Rendimiento: se ejecuta UNA sola vez por política, al compilarla — no en cada
    /// evaluación — evitando reparsear JSON en la ruta caliente de EvaluateAsync.
    /// </summary>
    internal static class JsonValueNormalizer
    {
        public static object? Normalize(object? raw)
        {
            if (raw is not JsonElement element)
                return raw;

            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(item => Normalize(item)).ToList(),
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(prop => prop.Name, prop => Normalize(prop.Value)),
                _ => throw new PolicyCompilationException($"Tipo de valor JSON no soportado: {element.ValueKind}.")
            };
        }
    }
}
