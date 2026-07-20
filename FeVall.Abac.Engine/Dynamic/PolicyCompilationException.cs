// FeVall.Abac.Engine/Dynamic/PolicyCompilationException.cs
namespace FeVall.Abac.Engine.Dynamic
{

    /// <summary>
    /// Señala que una PolicyDefinition no pudo compilarse: operador no permitido,
    /// ruta de atributo inválida, o estructura incoherente. Se lanza en tiempo de
    /// carga/publicación, nunca durante EvaluateAsync — el objetivo es rechazar la
    /// política inválida ANTES de que llegue a producción, no fallar en runtime.
    /// </summary>
    public sealed class PolicyCompilationException : Exception
    {
        public PolicyCompilationException(string message) : base(message) { }
        public PolicyCompilationException(string message, Exception inner) : base(message, inner) { }
    }
}
