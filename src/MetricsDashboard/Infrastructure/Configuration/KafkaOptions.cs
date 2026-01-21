namespace MetricsDashboard.Infrastructure.Configuration;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string ClientId { get; set; } = "metrics-dashboard-local";
    public KafkaConsumerOptions Consumer { get; init; } = new();
    public KafkaTopicOptions Topics { get; init; } = new();
}

public sealed class KafkaConsumerOptions
{
    public string GroupId { get; set; } = "metrics-dashboard-consumer";
    public string AutoOffsetReset { get; init; } = "earliest";
    public bool EnableAutoCommit { get; init; } = false;
    public int MaxPollIntervalMs { get; init; } = 300000;
}

public sealed class KafkaTopicOptions
{
    public string AggregatesSite { get; init; } = "telemetry.agg.site.v1";
    public string AggregatesSubstation { get; init; } = "telemetry.agg.substation.v1";
    public string AggregatesRegion { get; init; } = "telemetry.agg.region.v1";
}
