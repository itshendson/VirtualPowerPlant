using Confluent.Kafka;
using Ingestion.Model;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

namespace Ingestion.Infrastructure.Messaging
{
    public class TelemetryIngestBackgroundService : BackgroundService
    {
        private readonly ILogger<TelemetryIngestBackgroundService> _logger;
        private readonly ITelemetryIngestBuffer _buffer;
        private readonly ITelemetryProducer _producer;

        public TelemetryIngestBackgroundService(ILogger<TelemetryIngestBackgroundService> logger, ITelemetryIngestBuffer buffer, ITelemetryProducer producer)
        {
            _logger = logger;
            _buffer = buffer;
            _producer = producer;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                BufferItem<TelemetryReadingRequest> item;

                try
                {
                    item = await _buffer.DequeueAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Telemetry sender stopping. Draining buffer.");
                    DrainBuffer();
                    break;
                }
                catch (ChannelClosedException)
                {
                    _logger.LogInformation("Telemetry buffer completed. Sender stopping.");
                    break;
                }

                SendBufferedItem(item);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _buffer.Complete();
            await base.StopAsync(cancellationToken);
        }

        private void DrainBuffer()
        {
            var drained = 0;

            while (_buffer.TryDequeue(out var item))
            {
                drained++;
                SendBufferedItem(item);
            }

            if (drained > 0)
            {
                _logger.LogInformation("Drained {DrainedCount} telemetry events before shutdown.", drained);
            }
        }

        private void SendBufferedItem(BufferItem<TelemetryReadingRequest> item)
        {
            try
            {
                _producer.Produce<string, TelemetryReadingRequest>(
                    topic: item.Topic,
                    key: item.Key,
                    value: item.Value,
                    deliveryHandler: report =>
                    {
                        if (report.Status != PersistenceStatus.Persisted || report.Error.IsError)
                        {
                            _logger.LogError("Failed to deliver buffered telemetry event. EventId: {EventId}, MeterId: {MeterId}, Error: {Error}", item.EventId, item.Value.MeterId, report.Error.Reason);
                            return;
                        }

                        _logger.LogDebug("Buffered telemetry event sent. EventId: {EventId}, MeterId: {MeterId}", item.EventId, item.Value.MeterId);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send buffered telemetry event. EventId: {EventId}, MeterId: {MeterId}", item.EventId, item.Value.MeterId);
            }
        }
    }
}
