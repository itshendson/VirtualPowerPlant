using Confluent.Kafka;
using Ingestion.Infrastructure.Configuration;
using Ingestion.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Ingestion.Infrastructure.Messaging
{
    public class TelemetryIngestBackgroundService : BackgroundService
    {
        private readonly ILogger<TelemetryIngestBackgroundService> _logger;
        private readonly ITelemetryIngestBuffer _buffer;
        private readonly ITelemetryProducer _producer;
        private readonly int _maxDeliveryAttempts;
        private readonly int _retryBackoffMs;
        private readonly int _retryBackoffMaxMs;

        public TelemetryIngestBackgroundService(
            ILogger<TelemetryIngestBackgroundService> logger,
            ITelemetryIngestBuffer buffer,
            ITelemetryProducer producer,
            IOptions<TelemetryIngestBufferOptions> bufferOptions)
        {
            _logger = logger;
            _buffer = buffer;
            _producer = producer;

            var options = bufferOptions.Value;
            _maxDeliveryAttempts = Math.Max(1, options.MaxDeliveryAttempts);
            _retryBackoffMs = Math.Max(0, options.RetryBackoffMs);
            _retryBackoffMaxMs = Math.Max(_retryBackoffMs, options.RetryBackoffMaxMs);
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

        private void SendBufferedItem(BufferItem<TelemetryReadingRequest> bufferItem)
        {
            try
            {
                var attemptNumber = bufferItem.Attempt + 1;

                _producer.Produce<string, TelemetryReadingRequest>(
                    topic: bufferItem.Topic,
                    key: bufferItem.Key,
                    value: bufferItem.Value,
                    deliveryHandler: report =>
                    {
                        if (report.Status != PersistenceStatus.Persisted || report.Error.IsError)
                        {
                            if (attemptNumber < _maxDeliveryAttempts)
                            {
                                var retryItem = bufferItem with { Attempt = bufferItem.Attempt + 1 };
                                var delay = GetRetryDelay();

                                ScheduleRetry(retryItem, delay);

                                _logger.LogWarning("Failed to deliver buffered telemetry event. Requeueing for attempt {Attempt}/{MaxAttempts}. EventId: {EventId}, MeterId: {MeterId}, Error: {Error}",
                                    retryItem.Attempt + 1,
                                    _maxDeliveryAttempts,
                                    bufferItem.EventId,
                                    bufferItem.Value.MeterId,
                                    report.Error.Reason);

                                return;
                            }

                            _logger.LogError("Failed to deliver buffered telemetry event. Dropping after {Attempt}/{MaxAttempts} attempts. EventId: {EventId}, MeterId: {MeterId}, Error: {Error}",
                                attemptNumber,
                                _maxDeliveryAttempts,
                                bufferItem.EventId,
                                bufferItem.Value.MeterId,
                                report.Error.Reason);
                            return;
                        }

                        _logger.LogDebug("Buffered telemetry event sent. EventId: {EventId}, MeterId: {MeterId}", bufferItem.EventId, bufferItem.Value.MeterId);
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send buffered telemetry event. EventId: {EventId}, MeterId: {MeterId}", bufferItem.EventId, bufferItem.Value.MeterId);
            }
        }

        private void ScheduleRetry(BufferItem<TelemetryReadingRequest> item, TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
            {
                TryRequeue(item);
                return;
            }

            _ = Task.Delay(delay).ContinueWith(_ => TryRequeue(item), TaskScheduler.Default);
        }

        private void TryRequeue(BufferItem<TelemetryReadingRequest> item)
        {
            if (!_buffer.TryEnqueue(item))
            {
                _logger.LogWarning("Retry buffer full. Dropping telemetry event after failed requeue. EventId: {EventId}, MeterId: {MeterId}", item.EventId, item.Value.MeterId);
            }
        }

        private TimeSpan GetRetryDelay()
        {
            if (_retryBackoffMs <= 0) return TimeSpan.Zero;

            var delayMs = Math.Min(_retryBackoffMs, _retryBackoffMaxMs);
            return TimeSpan.FromMilliseconds(delayMs);
        }
    }
}
