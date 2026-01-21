namespace Aggregation.Infrastructure.Configuration;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string ClientId { get; set; } = "aggregation-local";
    public KafkaConsumerOptions Consumer { get; init; } = new();
    public KafkaProducerOptions Producer { get; init; } = new();
    public KafkaTopicOptions Topics { get; init; } = new();
}

public sealed class KafkaConsumerOptions
{
    public string GroupId { get; set; } = "aggregation-consumer";
    public string AutoOffsetReset { get; init; } = "earliest";
    public bool EnableAutoCommit { get; init; } = false;
    public int MaxPollIntervalMs { get; init; } = 300000;
}

public sealed class KafkaProducerOptions
{
    public bool EnableIdempotence { get; init; } = true;
    public int LingerMs { get; init; } = 5;
    public int BatchSize { get; init; } = 32 * 1024;
    public string Acks { get; init; } = "all";
    public int MessageTimeoutMs { get; init; } = 3000;
}

public sealed class KafkaTopicOptions
{
    public string TelemetryRaw { get; init; } = "telemetry.raw.v1";
    public string AggregatesSite { get; init; } = "telemetry.agg.site.v1";
    public string AggregatesSubstation { get; init; } = "telemetry.agg.substation.v1";
    public string AggregatesRegion { get; init; } = "telemetry.agg.region.v1";
}
