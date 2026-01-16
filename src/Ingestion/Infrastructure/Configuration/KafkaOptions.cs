namespace Ingestion.Infrastructure.Configuration
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string ClientId { get; set; } = "ingest-api-local";
        public KafkaProducerOptions Producer { get; init; } = new();
        public KafkaTopicOptions Topics { get; init; } = new();
    }

    public class KafkaProducerOptions
    {
        public bool EnableIdempotence { get; init; } = true;
        public int LingerMs { get; init; } = 5;
        public int BatchSize { get; init; } = 32 * 1024;
        public string Acks { get; init; } = "all";
        public int MessageTimeoutMs { get; init; } = 3000;
    }

    public class KafkaTopicOptions
    {
        public string TelemetryRaw { get; init; } = "telemetry.raw.v1";
    }
}
