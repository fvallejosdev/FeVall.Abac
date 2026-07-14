// FeVall.Abac.Abstractions/IEvaluationContext.cs
namespace FeVall.Abac.Abstractions;

/// <summary>
/// Representa el contexto completo de una solicitud de evaluación ABAC.
/// ISP: expone exactamente lo que el motor necesita — ni más, ni menos.
/// El motor nunca conoce tipos concretos del dominio consumidor.
/// </summary>
public interface IEvaluationContext
{
    /// <summary>¿Quién solicita el acceso? Usuario, servicio, sistema.</summary>
    AttributeBag Subject { get; }

    /// <summary>¿Sobre qué recurso? Documento, endpoint, entidad.</summary>
    AttributeBag Resource { get; }

    /// <summary>¿Qué acción intenta realizar? Read, write, delete, custom.</summary>
    AttributeBag Action { get; }

    /// <summary>¿En qué condiciones? Hora, IP, dispositivo, región.</summary>
    AttributeBag Environment { get; }
}