
// FeVall.Abac.Abstractions/Audit/IAuditSink.cs
namespace FeVall.Abac.Abstractions.Audit
{
    /// <summary>
    /// Punto de entrada para registrar auditoría sin bloquear el hilo que evalúa la decisión.
    /// La implementación real (ChannelAuditSink) solo encola — un BackgroundService
    /// se encarga de persistir por lotes. SRP: esta interfaz no sabe nada de "cómo" ni "dónde"
    /// se guarda finalmente el registro.
    /// </summary>
    public interface IAuditSink
    {
        /// <summary>
        /// Encola un registro para persistencia asíncrona. Nunca debe hacer I/O síncrono
        /// ni esperar confirmación de base de datos — eso degradaría la latencia de EvaluateAsync.
        /// </summary>
        ValueTask WriteAsync(AuditEntry entry, CancellationToken ct = default);
    }

    /// <summary>
    /// Abstracción de la persistencia real por lotes (Elasticsearch, SQL, series temporales).
    /// Implementada por el consumidor de la librería — FeVall.Abac no conoce el almacén final.
    /// </summary>
    public interface IAuditBatchWriter
    {
        Task WriteBatchAsync(IReadOnlyList<AuditEntry> batch, CancellationToken ct = default);
    }
}
