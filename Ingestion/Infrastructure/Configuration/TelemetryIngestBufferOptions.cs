namespace Ingestion.Infrastructure.Configuration
{
    public class TelemetryIngestBufferOptions
    {
        public int Capacity { get; init; } = 10000;
    }
}
