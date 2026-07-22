// FeVall.Abac.Engine/Dynamic/AttributePathResolver.cs
using FeVall.Abac.Abstractions;
using System.Text.RegularExpressions;

namespace FeVall.Abac.Engine.Dynamic
{
    /// <summary>
    /// Traduce una ruta textual como "Subject.Department" o "Resource.Owner.Department"
    /// en una lectura contra el IEvaluationContext real. Solo conoce cuatro categorías
    /// (Subject/Resource/Action/Environment) — una lista blanca cerrada, no un acceso
    /// genérico por reflexión.
    /// Rutas anidadas: cada segmento intermedio DEBE resolver a otro AttributeBag
    /// (no a un objeto arbitrario) — el "salto" entre niveles nunca usa reflexión,
    /// usa el mismo AttributeBag.Get&lt;T&gt; tipado que el resto del motor.
    /// Saneamiento: cada segmento debe cumplir un patrón estricto de identificador;
    /// esto evita que una política inyecte caracteres que intenten desviar la
    /// construcción de expresiones o acceder a miembros no previstos.
    /// </summary>
    internal static class AttributePathResolver
    {
        // Categoría + uno o más segmentos de identificador, ej.:
        //   "Resource.Type"                    (1 segmento, caso original)
        //   "Resource.Owner.Department"         (2 segmentos, anidado)
        private static readonly Regex ValidPathPattern =
            new(@"^(Subject|Resource|Action|Environment)(\.[A-Za-z_][A-Za-z0-9_]*)+$",
                RegexOptions.Compiled);

        // Límite defensivo: una ruta con anidamiento absurdo (ej. generada por un bug
        // de la UI o un intento deliberado de abuso) se rechaza en COMPILACIÓN,
        // igual que el límite de profundidad recomendado para el árbol de condiciones.
        private const int MaxSegments = 10;

        /// <summary>Valida el formato en tiempo de COMPILACIÓN — se llama una sola vez por política.</summary>
        public static void EnsureValidFormat(string path)
        {
            if (!ValidPathPattern.IsMatch(path))
                throw new PolicyCompilationException(
                    $"Ruta de atributo inválida: '{path}'. " +
                    "Formato esperado: 'Subject|Resource|Action|Environment.Nombre[.Nombre...]'.");

            var segmentCount = path.Count(c => c == '.');
            if (segmentCount > MaxSegments)
                throw new PolicyCompilationException(
                    $"Ruta de atributo demasiado profunda: '{path}' " +
                    $"({segmentCount} niveles, máximo permitido {MaxSegments}).");
        }

        /// <summary>
        /// Resuelve el valor en tiempo de EVALUACIÓN — se llama por cada request.
        /// Si algún segmento intermedio no existe o no es un AttributeBag, la ruta
        /// se trata como "atributo ausente" (retorna null) en vez de lanzar —
        /// mismo comportamiento fail-safe que un atributo de un solo nivel ausente,
        /// consistente con el resto del motor (la excepción, si corresponde, la
        /// lanza el IComparisonOperator al recibir null, ej. GreaterThan).
        /// </summary>
        public static object? Resolve(IEvaluationContext context, string path)
        {
            var segments = path.Split('.');
            var bag = SelectRootBag(context, segments[0]);

            // Recorre los segmentos intermedios (todo menos el primero y el último).
            // Cada uno debe resolver a un AttributeBag anidado para continuar.
            for (var i = 1; i < segments.Length - 1; i++)
            {
                var next = bag.Get<AttributeBag>(segments[i]);
                if (next is null)
                    return null; // Cadena rota a mitad de camino = atributo ausente.

                bag = next;
            }

            var lastKey = segments[^1];
            return bag.Get<object>(lastKey);
        }

        private static AttributeBag SelectRootBag(IEvaluationContext context, string category) => category switch
        {
            "Subject" => context.Subject,
            "Resource" => context.Resource,
            "Action" => context.Action,
            "Environment" => context.Environment,
            _ => throw new PolicyCompilationException($"Categoría de atributo desconocida: '{category}'.")
        };
    }
}
