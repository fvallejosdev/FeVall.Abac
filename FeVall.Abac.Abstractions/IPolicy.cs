// FeVall.Abac.Abstractions/IPolicy.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Contrato mínimo que toda política ABAC debe cumplir.
/// ISP: una sola responsabilidad por interfaz.
/// LSP: cualquier implementación debe poder sustituir a otra
///      sin que el motor cambie de comportamiento.
/// </summary>
public interface IPolicy
{
    /// <summary>
    /// Nombre único de la política. Usado en logging y diagnóstico.
    /// Clean Code: identifica la política sin necesidad de inspeccionar su lógica.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evalúa el contexto y retorna una decisión.
    /// NUNCA lanza excepciones como flujo de control — siempre retorna Decision.
    /// Si ocurre un error interno, retorna Decision.DenyWith(reason).
    /// </summary>
    Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default);
}