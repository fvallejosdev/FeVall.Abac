// FeVall.Abac.Abstractions/Dynamic/IComparisonOperator.cs

namespace FeVall.Abac.Abstractions.Dynamic
{
    /// <summary>
    /// Un operador de comparación disponible para políticas dinámicas.
    /// OCP: agregar un operador nuevo = una clase nueva + registrarla en DI.
    /// Nunca se modifica el compilador ni el árbol de condiciones.
    /// Seguridad: esta es la ÚNICA superficie de "lógica" que una política dinámica
    /// puede invocar. No hay eval() ni reflexión — solo estas implementaciones concretas
    /// y registradas explícitamente, es decir, una lista blanca real.
    /// </summary>
    public interface IComparisonOperator
    {
        /// <summary>Nombre exacto tal como aparece en el JSON de la política (ej. "Equals", "In").</summary>
        string Name { get; }

        /// <summary>
        /// Indica si el "Value" de la condición debe resolverse como una ruta de atributo
        /// (ej. "Resource.ProjectId") en lugar de tratarse como literal.
        /// Usado por operadores como ContainsAttribute que comparan dos atributos del contexto.
        /// </summary>
        bool ValueIsAttributeReference { get; }

        /// <summary>
        /// Compara el valor real (leído del contexto) contra el valor esperado
        /// (literal ya normalizado, o resuelto desde el contexto si ValueIsAttributeReference).
        /// Puede lanzar si los tipos son incompatibles — el llamador (FaultTolerantPolicyDecorator)
        /// se encarga de convertir cualquier excepción en un Deny seguro.
        /// </summary>
        bool Evaluate(object? actualValue, object? expectedValue);
    }
}
