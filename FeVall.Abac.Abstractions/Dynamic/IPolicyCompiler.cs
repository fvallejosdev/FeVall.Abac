// FeVall.Abac.Abstractions/Dynamic/IPolicyCompiler.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Compila una PolicyDefinition (datos) en un IPolicy (comportamiento).
    /// SRP: separa "cómo se guarda la política" de "cómo se evalúa".
    /// DIP: DynamicPolicyCache depende de esta abstracción, no de JsonPolicyCompiler directamente.
    /// </summary>
    public interface IPolicyCompiler
    {
        /// <summary>
        /// Compila una definición. Lanza PolicyCompilationException si la política
        /// referencia un operador no permitido, una ruta de atributo inválida,
        /// o tiene una estructura incoherente (ej. Not con más de un hijo).
        /// El llamador decide si una política inválida bloquea el arranque o solo se descarta.
        /// </summary>
        IPolicy Compile(PolicyDefinition definition);
    }
}
