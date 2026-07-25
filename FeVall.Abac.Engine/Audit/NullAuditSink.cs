// FeVall.Abac.Engine/Audit/NullAuditSink.cs
using FeVall.Abac.Abstractions.Audit;

namespace FeVall.Abac.Engine.Audit
{
    /// <summary>
    /// Implementación por defecto de IAuditSink cuando el consumidor no llamó
    /// AddAsyncAuditLog(). Sin esto, AbacEngine necesitaría resolver IAuditSink
    /// como opcional (IAuditSink? con lógica condicional dispersa) — en su lugar,
    /// siempre hay un IAuditSink registrado, y este simplemente no hace nada.
    /// Null Object Pattern: evita checks de null en el hot path de EvaluateAsync.
    /// internal sealed: detalle de implementación del motor.
    /// </summary>
    internal sealed class NullAuditSink : IAuditSink
    {
        public ValueTask WriteAsync(AuditEntry entry, CancellationToken ct = default) =>
           ValueTask.CompletedTask;
    }
}
