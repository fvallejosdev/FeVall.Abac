// FeVall.Abac.Engine/Audit/ChannelAuditSink.cs

using FeVall.Abac.Abstractions.Audit;
using System.Threading.Channels;

namespace FeVall.Abac.Engine.Audit
{
    /// <summary>
    /// IAuditSink respaldado por System.Threading.Channels. WriteAsync solo encola —
    /// nunca toca la base de datos ni el disco directamente, así que no agrega
    /// latencia perceptible a EvaluateAsync. AuditPersistenceWorker (BackgroundService)
    /// es quien realmente escribe, por lotes, en el almacén final.
    /// Bounded channel con BoundedChannelFullMode.DropOldest: bajo carga extrema,
    /// se prioriza no bloquear las evaluaciones de acceso sobre no perder el
    /// registro de auditoría más antiguo en el buffer — una decisión explícita de
    /// disponibilidad sobre completitud del log, documentada aquí para quien la revise.
    /// internal sealed: se registra como IAuditSink vía DI.
    /// </summary>
    internal sealed class ChannelAuditSink : IAuditSink
    {
        private readonly Channel<AuditEntry> _channel;

        public ChannelAuditSink(int capacity = 10_000)
        {
            _channel = Channel.CreateBounded<AuditEntry>(new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public ChannelReader<AuditEntry> Reader => _channel.Reader;

        public ValueTask WriteAsync(AuditEntry entry, CancellationToken ct = default) =>
            _channel.Writer.WriteAsync(entry, ct);
    }
}
