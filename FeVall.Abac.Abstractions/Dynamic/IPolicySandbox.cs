
// FeVall.Abac.Abstractions/Dynamic/IPolicySandbox.cs
namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Punto de entrada para probar una PolicyDefinition SIN publicarla — nunca toca
    /// IPolicyRepository ni DynamicPolicyCache. Es lo que la UI de administración
    /// llama en el botón "Probar" antes de "Guardar".
    /// SRP: compila + evalúa contra un contexto dado, nada más.
    /// DIP: reutiliza IPolicyCompiler existente — no duplica lógica de compilación.
    /// </summary>
    public interface IPolicySandbox
    {
        /// <summary>
        /// Compila la definición (sin cachear ni persistir) y la evalúa contra
        /// cada caso de prueba. Si la compilación falla, retorna un solo resultado
        /// con el error de compilación y ningún trace (nunca llega a evaluar).
        /// </summary>
        Task<PolicyTestReport> TestAsync(
            PolicyDefinition definition,
            IReadOnlyList<IEvaluationContext> testCases,
            CancellationToken ct = default);
    }
}
