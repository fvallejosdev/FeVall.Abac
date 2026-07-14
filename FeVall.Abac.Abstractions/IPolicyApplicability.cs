// FeVall.Abac.Abstractions/IPolicyApplicability.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Interfaz opcional. Implementar solo cuando una política
/// necesita declarar explícitamente si aplica al contexto dado.
/// El evaluador la usa para filtrar antes de llamar EvaluateAsync.
/// ISP: separada de IPolicy para no obligar a todas las políticas
///      a implementar lógica de aplicabilidad que no necesitan.
/// </summary>
public interface IPolicyApplicability
{
    bool IsApplicableTo(IEvaluationContext context);
}