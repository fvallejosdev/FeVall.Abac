// FeVall.Abac.Engine/Audit/AuditPersistenceWorker.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Audit;
using Microsoft.Extensions.Hosting;

namespace FeVall.Abac.Engine.Audit
{
    /// <summary>
    /// BackgroundService que drena ChannelAuditSink y persiste en lotes (batching)
    /// vía IAuditBatchWriter — implementado por el consumidor (Elasticsearch, SQL, etc.).
    /// SRP: esta clase solo sabe "leer del canal y agrupar"; el "dónde se guarda"
    /// es responsabilidad exclusiva de IAuditBatchWriter.
    /// Resiliencia: un fallo transitorio de IAuditBatchWriter (timeout de red, conexión
    /// caída) NUNCA debe matar la tarea de fondo completa — mismo criterio de
    /// reconexión/aislamiento aplicado a ListenForInvalidationsAsync (fix #8 de
    /// CHANGELOG_2.md). El lote que falla se pierde (auditoría es fail-open por
    /// diseño, ver AbacEngine.TryWriteAuditEntryAsync y DropOldest en ChannelAuditSink),
    /// pero el worker sigue vivo para procesar los lotes siguientes.
    /// </summary>
    internal sealed class AuditPersistenceWorker : BackgroundService
    {
        private readonly ChannelAuditSink _sink;
        private readonly IAuditBatchWriter _writer;
        private readonly IAbacLogger _logger;
        private readonly TimeSpan _flushInterval;
        private readonly int _maxBatchSize;

        public AuditPersistenceWorker(
            ChannelAuditSink sink,
            IAuditBatchWriter writer,
            IAbacLogger logger,
            TimeSpan? flushInterval = null,
            int maxBatchSize = 500)
        {
            _sink = sink;
            _writer = writer;
            _logger = logger;
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
                    await TryWriteBatchAsync(batch, stoppingToken);

                await flushTimer.WaitForNextTickAsync(stoppingToken);
            }
        }

        // Aislamiento de fallos por lote — mismo criterio que el fix #8 de
        // ListenForInvalidationsAsync: un fallo transitorio de IAuditBatchWriter
        // NUNCA debe matar el BackgroundService completo.
        private async Task TryWriteBatchAsync(List<AuditEntry> batch, CancellationToken ct)
        {
            try
            {
                await _writer.WriteBatchAsync(batch, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw; // Apagado normal del servicio — sí debe propagar.
            }
            catch (Exception ex)
            {
                _logger.LogInfrastructureFault(nameof(AuditPersistenceWorker), ex);
            }
        }
    }
}