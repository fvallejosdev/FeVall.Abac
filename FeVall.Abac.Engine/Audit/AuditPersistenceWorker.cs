// FeVall.Abac.Engine/Audit/AuditPersistenceWorker.cs
using FeVall.Abac.Abstractions.Audit;
using Microsoft.Extensions.Hosting;


namespace FeVall.Abac.Engine.Audit
{
    /// <summary>
    /// BackgroundService que drena ChannelAuditSink y persiste en lotes (batching)
    /// vía IAuditBatchWriter — implementado por el consumidor (Elasticsearch, SQL, etc.).
    /// SRP: esta clase solo sabe "leer del canal y agrupar"; el "dónde se guarda"
    /// es responsabilidad exclusiva de IAuditBatchWriter.
    /// </summary>
    internal sealed class AuditPersistenceWorker : BackgroundService
    {
        private readonly ChannelAuditSink _sink;
        private readonly IAuditBatchWriter _writer;
        private readonly TimeSpan _flushInterval;
        private readonly int _maxBatchSize;

        public AuditPersistenceWorker(
            ChannelAuditSink sink,
            IAuditBatchWriter writer,
            TimeSpan? flushInterval = null,
            int maxBatchSize = 500)
        {
            _sink = sink;
            _writer = writer;
            _flushInterval = flushInterval ?? TimeSpan.FromSeconds(2);
            _maxBatchSize = maxBatchSize;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var batch = new List<AuditEntry>(_maxBatchSize);
            using var flushTimer = new PeriodicTimer(_flushInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                batch.Clear();

                // Drena todo lo disponible sin esperar, hasta el tamaño máximo del lote.
                while (batch.Count < _maxBatchSize && _sink.Reader.TryRead(out var entry))
                    batch.Add(entry);

                if (batch.Count > 0)
                    await _writer.WriteBatchAsync(batch, stoppingToken);

                await flushTimer.WaitForNextTickAsync(stoppingToken);
            }
        }
    }
}
