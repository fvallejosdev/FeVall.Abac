// FeVall.Abac.Engine/Dynamic/AttributePathResolver.cs
using FeVall.Abac.Abstractions;
using System.Text.RegularExpressions;

namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// Traduce una ruta textual como "Subject.Department" en una lectura contra el
    /// IEvaluationContext real. Solo conoce cuatro categorías (Subject/Resource/Action/Environment) —
    /// una lista blanca cerrada, no un acceso genérico por reflexión.
    /// Saneamiento: la clave debe cumplir un patrón estricto de identificador;
    /// esto evita que una política inyecte caracteres que intenten desviar la
    /// construcción de expresiones o acceder a miembros no previstos.
    /// </summary>
    internal static class AttributePathResolver
    {
        private static readonly Regex ValidPathPattern =
          new(@"^(Subject|Resource|Action|Environment)\.[A-Za-z_][A-Za-z0-9_]*$",
             RegexOptions.Compiled);
        /// <summary>Valida el formato en tiempo de COMPILACIÓN — se llama una sola vez por política.</summary>
        public static void EnsureValidFormat(string path)
        {
            if (!ValidPathPattern.IsMatch(path))
                throw new PolicyCompilationException(
                    $"Ruta de atributo inválida: '{path}'. " +
                    "Formato esperado: 'Subject|Resource|Action|Environment.NombreDeAtributo'.");
        }

        /// <summary>Resuelve el valor en tiempo de EVALUACIÓN — se llama por cada request.</summary>
        public static object? Resolve(IEvaluationContext context, string path)
        {
            var separatorIndex = path.IndexOf('.');
            var category = path[..separatorIndex];
            var key = path[(separatorIndex + 1)..];

            var bag = category switch
            {
                "Subject" => context.Subject,
                "Resource" => context.Resource,
                "Action" => context.Action,
                "Environment" => context.Environment,
                _ => throw new PolicyCompilationException($"Categoría de atributo desconocida: '{category}'.")
            };

            return bag.Get<object>(key);
        }
    }
}
