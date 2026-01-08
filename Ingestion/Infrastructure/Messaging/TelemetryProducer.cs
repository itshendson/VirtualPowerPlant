
using Confluent.Kafka;
using System.Text.Json;

namespace Ingestion.Infrastructure.Messaging
{
    public class TelemetryProducer : ITelemetryProducer, IAsyncDisposable, IDisposable
    {
        private readonly ILogger<TelemetryProducer> _logger;
        private readonly IProducer<string, byte[]> _producer;

        public TelemetryProducer(ILogger<TelemetryProducer> logger, ProducerConfig config)
        {
            _logger = logger;
            _producer = new ProducerBuilder<string, byte[]>(config).Build();
        }

        public async Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Headers? headers, CancellationToken cancellationToken = default)
        {
            var keyString = key?.ToString() ?? string.Empty;
            var payload = JsonSerializer.SerializeToUtf8Bytes(value);

            _logger.LogDebug("Producing message to topic {Topic} with key {Key} and payload size {PayloadSize} bytes", topic, keyString, payload.Length);

            await _producer.ProduceAsync(topic, new Message<string, byte[]>
            {
                Key = keyString,
                Value = payload,
                Headers = headers
            }, cancellationToken);
        }

        public void Produce<TKey, TValue>(string topic, TKey key, TValue value, Headers? headers = null, Action<DeliveryReport<string, byte[]>>? deliveryHandler = null)
        {
            var keyString = key?.ToString() ?? string.Empty;
            var payload = JsonSerializer.SerializeToUtf8Bytes(value);

            _logger.LogDebug("Producing message to topic {Topic} with key {Key} and payload size {PayloadSize} bytes", topic, keyString, payload.Length);

            _producer.Produce(topic, new Message<string, byte[]>
            {
                Key = keyString,
                Value = payload,
                Headers = headers
            }, deliveryHandler);
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
                _logger.LogInformation("Kafka producer flushed and disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while flushing Kafka producer");
            }
            finally
            {
                _producer.Dispose();
            }

            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while flushing Kafka producer during synchronous disposal");
            }
            finally
            {
                _producer.Dispose();
            }
        }
    }
}
