using System;
using System.Collections.Generic;
using System.Text;
// FeVall.Abac.Abstractions/Dynamic/IExplainableConditionNode.cs
namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Capacidad opcional de un IConditionNode para explicar su propio resultado
    /// con el detalle de qué condición específica se cumplió o falló.
    /// ISP: separada de IConditionNode para no obligar a evaluar-y-explicar
    /// siempre juntos — IsSatisfiedBy sigue siendo la ruta rápida con cortocircuito;
    /// Explain es la ruta de diagnóstico, deliberadamente sin cortocircuito,
    /// para que el usuario de la UI vea el árbol completo, no solo la primera rama que falló.
    /// </summary>
    public interface IExplainableConditionNode : IConditionNode
    {
        ConditionTrace Explain(IEvaluationContext context);
    }
}
