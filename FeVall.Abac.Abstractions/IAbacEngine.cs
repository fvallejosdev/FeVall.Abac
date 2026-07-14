// FeVall.Abac.Abstractions/IAbacEngine.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Punto de entrada único al motor ABAC.
/// DIP: el consumidor depende de esta abstracción, nunca de AbacEngine directamente.
/// SRP: declara una sola operación — evaluar un contexto y retornar una decisión.
/// </summary>
public interface IAbacEngine
{
    /// <summary>
    /// Evalúa el contexto dado contra todas las políticas registradas
    /// y retorna una decisión final.
    /// Nunca retorna null — garantizado por el contrato.
    /// </summary>
    Task<Decision> EvaluateAsync(
        IEvaluationContext context,
        CancellationToken ct = default);
}