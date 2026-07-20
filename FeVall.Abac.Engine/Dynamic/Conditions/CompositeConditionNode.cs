// FeVall.Abac.Engine/Dynamic/Conditions/CompositeConditionNode.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Dynamic;
using FeVall.Abac.Engine.Dynamic;

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
    internal sealed class CompositeConditionNode : IConditionNode
    {
        private readonly LogicalOperator _operator;
        private readonly IReadOnlyList<IConditionNode> _children;

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
    }
}
