using Confluent.Kafka;

namespace Ingestion.Infrastructure.Messaging
{
    public interface ITelemetryProducer
    {
        Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value, Headers? headers = null, CancellationToken cancellationToken = default);
    }
}
