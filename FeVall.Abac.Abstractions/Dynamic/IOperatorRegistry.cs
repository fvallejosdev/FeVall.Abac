// FeVall.Abac.Abstractions/Dynamic/IOperatorRegistry.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Resuelve el nombre textual de un operador (tal como viene en el JSON) contra
    /// la lista blanca de IComparisonOperator registrados en DI.
    /// DIP: el compilador depende de esta abstracción, nunca de un switch hardcodeado.
    /// </summary>
    public interface IOperatorRegistry
    {
        /// <summary>
        /// Retorna el operador registrado con ese nombre.
        /// Lanza NotSupportedException si el operador no está en la lista blanca —
        /// esto invalida la carga de la política completa (fail-closed en tiempo de compilación).
        /// </summary>
        IComparisonOperator Resolve(string operatorName);
    }
}
