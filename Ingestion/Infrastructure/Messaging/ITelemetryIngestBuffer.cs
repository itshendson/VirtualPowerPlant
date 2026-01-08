namespace Ingestion.Infrastructure.Messaging
{
    public interface ITelemetryIngestBuffer
    {
        bool TryEnqueue(TelemetryIngestionWorkItem item);
        ValueTask<TelemetryIngestionWorkItem> DequeueAsync(CancellationToken cancellationToken);
    }
}
