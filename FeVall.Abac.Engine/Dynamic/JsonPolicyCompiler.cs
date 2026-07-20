// FeVall.Abac.Engine/Dynamic/JsonPolicyCompiler.cs
using System.Text.Json;
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;
using FeVall.Abac.Engine.Dynamic.Conditions;

namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Único punto donde una PolicyDefinition "de datos" se convierte en un
    /// IConditionNode "de comportamiento". Toda la seguridad contra inyección vive
    /// aquí: lista blanca de operadores (IOperatorRegistry) y saneamiento de rutas
    /// de atributo (AttributePathResolver.EnsureValidFormat).
    /// internal sealed: se expone únicamente a través de IPolicyCompiler.
    /// </summary>
    internal sealed class JsonPolicyCompiler : IPolicyCompiler
    {
        private readonly IOperatorRegistry _operators;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public JsonPolicyCompiler(IOperatorRegistry operators)
        {
            ArgumentNullException.ThrowIfNull(operators);
            _operators = operators;
        }

        /// <summary>Punto de entrada alternativo: compila directo desde el JSON crudo de la UI.</summary>
        public IPolicy CompileFromJson(string json)
        {
            var definition = JsonSerializer.Deserialize<PolicyDefinition>(json, JsonOptions)
                ?? throw new PolicyCompilationException("El JSON de la política no pudo deserializarse.");

            return Compile(definition);
        }

        public IPolicy Compile(PolicyDefinition definition)
        {
            ValidateShape(definition);

            var target = definition.Target is null ? null : BuildNode(definition.Target);
            var rule = BuildNode(definition.Rule);

            return new CompiledPolicy(definition.Name, target, rule, definition.Obligations);
        }

        private static void ValidateShape(PolicyDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.PolicyId))
                throw new PolicyCompilationException("PolicyId es obligatorio.");

            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new PolicyCompilationException("Name es obligatorio.");
        }

        private IConditionNode BuildNode(ConditionDefinition def)
        {
            if (def.IsLeaf)
                return BuildLeaf(def);

            if (def.Conditions.Count == 0)
                throw new PolicyCompilationException(
                    "Un nodo compuesto debe declarar al menos una condición hija.");

            var logicalOperator = ParseLogicalOperator(def.Operator);
            var children = def.Conditions.Select(BuildNode).ToList();

            return new CompositeConditionNode(logicalOperator, children);
        }

        private IConditionNode BuildLeaf(ConditionDefinition def)
        {
            if (string.IsNullOrWhiteSpace(def.Operator))
                throw new PolicyCompilationException(
                    $"La condición sobre '{def.Attribute}' no declara un Operator.");

            AttributePathResolver.EnsureValidFormat(def.Attribute!);

            var comparisonOperator = _operators.Resolve(def.Operator);
            var expectedValue = NormalizeExpectedValue(def, comparisonOperator);

            return new AttributeConditionNode(def.Attribute!, comparisonOperator, expectedValue);
        }

        private static object? NormalizeExpectedValue(ConditionDefinition def, IComparisonOperator op)
        {
            if (op.ValueIsAttributeReference)
            {
                // El "Value" es en realidad otra ruta de atributo (ej. "Resource.ProjectId")
                // y se sanea igual que cualquier otra ruta — no como literal.
                var referencePath = def.Value?.ToString()
                    ?? throw new PolicyCompilationException(
                        $"El operador '{op.Name}' requiere que Value sea una ruta de atributo.");

                AttributePathResolver.EnsureValidFormat(referencePath);
                return referencePath;
            }

            return JsonValueNormalizer.Normalize(def.Value);
        }

        // "And" es el default cuando el JSON omite Operator en un nodo con Conditions
        // (así está modelado el bloque "Target" del ejemplo de M&A).
        private static LogicalOperator ParseLogicalOperator(string? raw) => raw switch
        {
            null or "" or "And" => LogicalOperator.And,
            "Or" => LogicalOperator.Or,
            "Not" => LogicalOperator.Not,
            _ => throw new PolicyCompilationException(
                     $"Operador lógico '{raw}' no está en la lista blanca (And, Or, Not).")
        };
    }
}
