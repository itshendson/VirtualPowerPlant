namespace Ingestion.Infrastructure.Configuration
{
    public class TelemetryIngestBufferOptions
    {
        public int Capacity { get; init; } = 10000;
        public int MaxDeliveryAttempts { get; init; } = 3;
        public int RetryBackoffMs { get; init; } = 100;
        public int RetryBackoffMaxMs { get; init; } = 2000;
    }
}
