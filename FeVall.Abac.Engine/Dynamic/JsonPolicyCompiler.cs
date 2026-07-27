// FeVall.Abac.Engine/Dynamic/JsonPolicyCompiler.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;
using FeVall.Abac.Engine.Dynamic.Conditions;
using FeVall.Abac.Engine.Extensions;
using System.Text.Json;


namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Único punto donde una PolicyDefinition "de datos" se convierte en un
    /// IConditionNode "de comportamiento". Toda la seguridad contra inyección vive
    /// aquí: lista blanca de operadores (IOperatorRegistry) y saneamiento de rutas
    /// de atributo (AttributePathResolver.EnsureValidFormat).
    /// internal sealed: se expone únicamente a través de IPolicyCompiler.
    /// </summary>
    // FeVall.Abac.Engine/Dynamic/JsonPolicyCompiler.cs
    internal sealed class JsonPolicyCompiler : IPolicyCompiler
    {
        private readonly IOperatorRegistry _operators;
        private readonly AbacEngineOptions _options;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static readonly HashSet<string> ValueRequiredOperators = new(StringComparer.Ordinal)
        {
            "Equals", "NotEquals", "GreaterThan", "LessThan", "GreaterThanOrEqual", "LessThanOrEqual",
            "In", "NotIn", "ContainsAttribute", "NotContainsAttribute",
            "StartsWith", "EndsWith", "ContainsText",
            "Between", "NotBetween", "DateAfter", "DateBefore", "DateBetween"
        };

        private static readonly HashSet<string> ValidFulfillOnValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "Permit", "Deny"
        };

        public JsonPolicyCompiler(IOperatorRegistry operators, AbacEngineOptions options)
        {
            ArgumentNullException.ThrowIfNull(operators);
            ArgumentNullException.ThrowIfNull(options);
            _operators = operators;
            _options = options;
        }

        public IPolicy CompileFromJson(string json, bool? explainOnDeny = null)
        {
            var definition = JsonSerializer.Deserialize<PolicyDefinition>(json, JsonOptions)
                ?? throw new PolicyCompilationException("El JSON de la política no pudo deserializarse.");
            return Compile(definition, explainOnDeny);
        }

        public IPolicy Compile(PolicyDefinition definition, bool? explainOnDeny = null)
        {
            ValidateShape(definition);

            var target = definition.Target is null ? null : BuildNode(definition.Target);
            var rule = BuildNode(definition.Rule);

            var permitObligations = BuildObligations(definition.Obligations, fulfillOn: "Permit");
            var denyObligations = BuildObligations(definition.Obligations, fulfillOn: "Deny");

            // Si el llamador no fuerza un valor (ej. PolicySandbox sí lo fuerza a true),
            // se respeta la configuración de producción: solo se paga el costo de
            // Explain (recorrido completo del árbol sin cortocircuito) cuando el
            // consumidor activó logging detallado.
            var effectiveExplainOnDeny = explainOnDeny ?? _options.EnableDetailedLogging;

            return new CompiledPolicy(
                definition.Name, target, rule, permitObligations, denyObligations, effectiveExplainOnDeny);
        }

        // Normaliza los Parameters (JsonElement → tipos CLR) UNA vez, en compilación —
        // igual criterio de rendimiento que JsonValueNormalizer aplica a Value.
        private static IReadOnlyList<Obligation> BuildObligations(
            IReadOnlyList<ObligationDefinition> definitions, string fulfillOn) =>
            definitions
                .Where(o => string.Equals(o.FulfillOn, fulfillOn, StringComparison.OrdinalIgnoreCase))
                .Select(o => new Obligation
                {
                    Id = o.Id,
                    Parameters = o.Parameters.ToDictionary(
                        kv => kv.Key,
                        kv => JsonValueNormalizer.Normalize(kv.Value))
                })
                .ToArray();

        private static void ValidateShape(PolicyDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.PolicyId))
                throw new PolicyCompilationException("PolicyId es obligatorio.");

            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new PolicyCompilationException("Name es obligatorio.");

            ValidateObligations(definition.Obligations);
        }

        // (4) Rechaza FulfillOn fuera de la lista blanca — evita obligaciones
        // que nunca se disparan por un typo silencioso.
        private static void ValidateObligations(IReadOnlyList<ObligationDefinition> obligations)
        {
            foreach (var obligation in obligations)
            {
                if (string.IsNullOrWhiteSpace(obligation.Id))
                    throw new PolicyCompilationException("Toda Obligation debe declarar un Id.");

                if (!ValidFulfillOnValues.Contains(obligation.FulfillOn))
                    throw new PolicyCompilationException(
                        $"La obligación '{obligation.Id}' tiene FulfillOn='{obligation.FulfillOn}' inválido. " +
                        "Valores permitidos: 'Permit', 'Deny'.");
            }
        }

        private IConditionNode BuildNode(ConditionDefinition def)
        {
            var hasAttribute = def.Attribute is not null;
            var hasConditions = def.Conditions.Count > 0;

            // (1) Ambigüedad estructural: un nodo no puede ser hoja Y rama al mismo tiempo.
            if (hasAttribute && hasConditions)
                throw new PolicyCompilationException(
                    $"Nodo ambiguo: declara Attribute ('{def.Attribute}') y Conditions al mismo tiempo. " +
                    "Un nodo debe ser una hoja (Attribute) o un nodo compuesto (Conditions), nunca ambos.");

            return hasAttribute ? BuildLeaf(def) : BuildComposite(def);
        }

        private IConditionNode BuildLeaf(ConditionDefinition def)
        {
            if (string.IsNullOrWhiteSpace(def.Operator))
                throw new PolicyCompilationException(
                    $"La condición sobre '{def.Attribute}' no declara un Operator.");

            // Un operador lógico (And/Or/Not) en una hoja es otra forma de la misma ambigüedad:
            // indica que el autor quiso un nodo compuesto pero olvidó Conditions, o viceversa.
            if (def.Operator is "And" or "Or" or "Not")
                throw new PolicyCompilationException(
                    $"La condición sobre '{def.Attribute}' usa el operador lógico '{def.Operator}', " +
                    "pero los operadores lógicos solo son válidos en nodos compuestos (con Conditions, sin Attribute).");

            AttributePathResolver.EnsureValidFormat(def.Attribute!);

            var comparisonOperator = _operators.Resolve(def.Operator);

            // (2) Value ausente en un operador que lo requiere — se detecta en COMPILACIÓN,
            // no se deja para que reviente en el primer request real.
            if (def.Value is null && ValueRequiredOperators.Contains(def.Operator))
                throw new PolicyCompilationException(
                    $"La condición sobre '{def.Attribute}' usa el operador '{def.Operator}', " +
                    "que requiere un Value, pero Value es null o no fue declarado.");

            var expectedValue = NormalizeExpectedValue(def, comparisonOperator);

            return new AttributeConditionNode(def.Attribute!, comparisonOperator, expectedValue, def.Description);
        }

        private IConditionNode BuildComposite(ConditionDefinition def)
        {
            // (3) Nodo compuesto vacío — ni Attribute ni Conditions con elementos.
            if (def.Conditions.Count == 0)
                throw new PolicyCompilationException(
                    (def.Description is { Length: > 0 } desc ? $"'{desc}': " : "") +
                    "Un nodo compuesto debe declarar al menos una condición hija, o ser una hoja con Attribute.");

            var logicalOperator = ParseLogicalOperator(def.Operator);
            var children = def.Conditions.Select(BuildNode).ToList();

            return new CompositeConditionNode(logicalOperator, children, def.Description);
        }

        private static object? NormalizeExpectedValue(ConditionDefinition def, IComparisonOperator op)
        {
            if (op.ValueIsAttributeReference)
            {
                var referencePath = def.Value?.ToString()
                    ?? throw new PolicyCompilationException(
                        $"El operador '{op.Name}' requiere que Value sea una ruta de atributo.");

                AttributePathResolver.EnsureValidFormat(referencePath);
                return referencePath;
            }

            var normalized = JsonValueNormalizer.Normalize(def.Value);

            // Validación de FORMA en compilación (conteo de elementos, min<=max,
            // parseabilidad numérica/fecha) — solo para operadores que la declaran
            // (Between, NotBetween, DateBetween hoy; OCP: cualquier operador futuro
            // que implemente IValueValidatingOperator queda cubierto automáticamente
            // sin tocar este método de nuevo).
            if (op is IValueValidatingOperator validating)
            {
                try
                {
                    validating.ValidateValueShape(normalized);
                }
                catch (Exception ex)
                {
                    throw new PolicyCompilationException(
                        $"Value inválido para el operador '{op.Name}': {ex.Message}", ex);
                }
            }

            return normalized;
        }

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
