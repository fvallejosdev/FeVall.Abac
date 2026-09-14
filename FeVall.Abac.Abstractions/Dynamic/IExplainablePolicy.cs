using System;
using System.Collections.Generic;
using System.Text;

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Capacidad opcional de un IPolicy compilado dinámicamente para reconstruir
    /// su ConditionTrace completo bajo demanda, sin importar el efecto (Permit
    /// o Deny) que produjo EvaluateAsync. ISP: separada de IPolicy — la
    /// evaluación de producción nunca necesita esto (Decision.Trace ya cubre
    /// el caso de Deny vía explainOnDeny). Solo IPolicySandbox la consume,
    /// para dar visibilidad total al usuario que prueba una política antes
    /// de publicarla — el mismo criterio de "el sandbox necesita ver más que
    /// producción" ya aplicado en PolicyTestCaseResult.RuntimeError.
    /// </summary>
    public interface IExplainablePolicy
    {
        /// <summary>
        /// Recorre el árbol de condiciones completo (sin cortocircuito) y lo
        /// traduce a ConditionTrace, independientemente de si el resultado
        /// final es Permit o Deny. Mismo costo que IExplainableConditionNode.Explain.
        /// </summary>
        ConditionTrace Explain(IEvaluationContext context);
    }
}
