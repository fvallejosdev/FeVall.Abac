// FeVall.Abac.Engine/Dynamic/Conditions/CompositeConditionNode.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;


namespace FeVall.Abac.Engine.Dynamic.Conditions
{

    internal enum LogicalOperator { And, Or, Not }

    /// <summary>
    /// Rama del árbol de condiciones: combina hijos con And/Or/Not.
    /// Cortocircuita internamente (And se detiene en el primer false, Or en el primer true) —
    /// esto es evaluación perezosa a nivel de condición, complementaria al cortocircuito
    /// a nivel de políticas que hace ShortCircuitPolicyEvaluator.
    /// internal sealed: solo JsonPolicyCompiler construye instancias de esto.
    /// </summary>
    internal sealed class CompositeConditionNode : IExplainableConditionNode
    {
        private readonly LogicalOperator _operator;
        private readonly IReadOnlyList<IConditionNode> _children;
        private readonly string? _description;

        public CompositeConditionNode(LogicalOperator @operator, IReadOnlyList<IConditionNode> children)
        {
            if (@operator is LogicalOperator.Not && children.Count != 1)
                throw new PolicyCompilationException(
                    $"El operador lógico 'Not' requiere exactamente una condición hija, se recibieron {children.Count}.");

            _operator = @operator;
            _children = children;
        }

        public bool IsSatisfiedBy(IEvaluationContext context) => _operator switch
        {
            LogicalOperator.And => _children.All(child => child.IsSatisfiedBy(context)),
            LogicalOperator.Or => _children.Any(child => child.IsSatisfiedBy(context)),
            LogicalOperator.Not => !_children[0].IsSatisfiedBy(context),
            _ => throw new PolicyCompilationException($"Operador lógico no soportado: {_operator}.")
        };

        public ConditionTrace Explain(IEvaluationContext context)
        {
            // Deliberado: NO cortocircuita. Evalúa todos los hijos para que el usuario
            // vea el panorama completo (ej. "estas 2 de 3 condiciones fallaron"),
            // no solo la primera que ya definió el resultado.
            var childTraces = _children
                .Select(child => ExplainChild(child, context))
                .ToList();

            var isSatisfied = _operator switch
            {
                LogicalOperator.And => childTraces.All(t => t.IsSatisfied),
                LogicalOperator.Or => childTraces.Any(t => t.IsSatisfied),
                LogicalOperator.Not => !childTraces[0].IsSatisfied,
                _ => throw new PolicyCompilationException($"Operador lógico no soportado: {_operator}.")
            };

            return new ConditionTrace
            {
                IsSatisfied = isSatisfied,
                Description = _description,
                LogicalOperator = _operator.ToString(),
                Children = childTraces
            };
        }


        // Fallback defensivo: si algún IConditionNode custom no implementa
        // IExplainableConditionNode (ej. uno que tú mismo escribas a mano),
        // igual se puede mostrar en el árbol sin romper el Explain completo.
        private static ConditionTrace ExplainChild(IConditionNode child, IEvaluationContext context) =>
            child is IExplainableConditionNode explainable
                ? explainable.Explain(context)
                : new ConditionTrace
                {
                    IsSatisfied = child.IsSatisfiedBy(context),
                    Description = "Nodo no explicable (IConditionNode custom sin IExplainableConditionNode)."
                };
    }
}
