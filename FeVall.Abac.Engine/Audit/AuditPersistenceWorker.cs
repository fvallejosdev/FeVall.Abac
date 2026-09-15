// FeVall.Abac.Engine/Audit/AuditPersistenceWorker.cs
using FeVall.Abac.Abstractions;
using FeVall.Abac.Abstractions.Audit;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                bool hasData;
                try
                {
                    // Espera reactivamente a que haya datos O a que se cumpla el
                    // flushInterval — lo que ocurra primero. Esto elimina el techo
                    // artificial: si hay backlog, el siguiente ciclo arranca de
                    // inmediato en vez de esperar el tick completo sin necesidad.
                    using var flushCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    flushCts.CancelAfter(_flushInterval);
                    hasData = await _sink.Reader.WaitToReadAsync(flushCts.Token);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break; // Apagado normal.
                }
                catch (OperationCanceledException)
                {
                    hasData = true; // Venció el flushInterval, no el shutdown: se revisa igual.
                }

                if (!hasData) continue;

                batch.Clear();
                while (batch.Count < _maxBatchSize && _sink.Reader.TryRead(out var entry))
                    batch.Add(entry);

                if (batch.Count > 0)
                    await TryWriteBatchAsync(batch, stoppingToken);
            }

            await DrainRemainingOnShutdownAsync(batch);

        }

        // Graceful shutdown: lo que quedó en el canal al cancelar el servicio
        // NUNCA debe descartarse en silencio — se persiste con best-effort usando
        // CancellationToken.None (el proceso ya está deteniéndose; esperar aquí
        // es preferible a perder el remanente).
        private async Task DrainRemainingOnShutdownAsync(List<AuditEntry> batch)
        {
            batch.Clear();
            while (_sink.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
                if (batch.Count < _maxBatchSize) continue;

                await TryWriteBatchAsync(batch, CancellationToken.None);
                batch.Clear();
            }

            if (batch.Count > 0)
                await TryWriteBatchAsync(batch, CancellationToken.None);
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