// FeVall.Abac.Abstractions/Dynamic/IPolicyCompiler.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    public interface IPolicyCompiler
    {
        /// <summary>
        /// Compila una definición. Lanza PolicyCompilationException si la política
        /// referencia un operador no permitido, una ruta de atributo inválida,
        /// o tiene una estructura incoherente (ej. Not con más de un hijo).
        /// </summary>
        /// <param name="explainOnDeny">
        /// Override explícito del costo de trazabilidad en Deny (recorrido completo
        /// del árbol, sin cortocircuito). Si es null, se usa el valor configurado
        /// en AbacEngineOptions.EnableDetailedLogging. IPolicySandbox SIEMPRE pasa
        /// true explícito — el usuario probando una política necesita ver el árbol
        /// completo sin importar la configuración de logging de producción.
        /// </param>
        IPolicy Compile(PolicyDefinition definition, bool? explainOnDeny = null);
    }
}
