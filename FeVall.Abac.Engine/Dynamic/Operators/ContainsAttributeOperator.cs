// FeVall.Abac.Engine/Dynamic/Operators/ContainsAttributeOperator.cs
using FeVall.Abac.Abstractions.Dynamic;

namespace FeVall.Abac.Engine.Dynamic.Operators
{

    /// <summary>
    /// Operador "ContainsAttribute": el atributo real es una colección (ej. Subject.ActiveProjects)
    /// y el "Value" NO es un literal sino la ruta de OTRO atributo (ej. "Resource.ProjectId") que
    /// debe resolverse desde el contexto. ValueIsAttributeReference=true le indica esto a
    /// AttributeConditionNode, que resuelve ambos lados antes de llamar a Evaluate.
    /// Este es exactamente el caso de "Subject.ActiveProjects ContainsAttribute Resource.ProjectId"
    /// del ejemplo de política de M&amp;A.
    /// </summary>
    internal sealed class ContainsAttributeOperator : IComparisonOperator
    {
        public string Name => "ContainsAttribute";
        public bool ValueIsAttributeReference => true;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            InOperator.AsEnumerable(actualValue).Any(item => AttributeValueComparer.AreEqual(item, expectedValue));
    }

    internal sealed class NotContainsAttributeOperator : IComparisonOperator
    {
        public string Name => "NotContainsAttribute";
        public bool ValueIsAttributeReference => true;

        public bool Evaluate(object? actualValue, object? expectedValue) =>
            !InOperator.AsEnumerable(actualValue).Any(item => AttributeValueComparer.AreEqual(item, expectedValue));
    }
}
