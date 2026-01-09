namespace Ingestion.Infrastructure.Messaging
{
    public interface ITelemetryIngestBuffer
    {
        bool TryEnqueue(TelemetryIngestionWorkItem item);
        ValueTask EnqueueAsync(TelemetryIngestionWorkItem item, CancellationToken cancellationToken);
        ValueTask<TelemetryIngestionWorkItem> DequeueAsync(CancellationToken cancellationToken);
    }
}
