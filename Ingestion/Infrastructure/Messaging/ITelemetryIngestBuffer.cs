using Ingestion.Model;

namespace Ingestion.Infrastructure.Messaging
{
    public interface ITelemetryIngestBuffer
    {
        bool TryEnqueue(BufferItem<TelemetryReadingRequest> item);
        ValueTask EnqueueAsync(BufferItem<TelemetryReadingRequest> item, CancellationToken cancellationToken);
        ValueTask<BufferItem<TelemetryReadingRequest>> DequeueAsync(CancellationToken cancellationToken);
        bool TryDequeue(out BufferItem<TelemetryReadingRequest> item);
        void Complete();
    }
}
