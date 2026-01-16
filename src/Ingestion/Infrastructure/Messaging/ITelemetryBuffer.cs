using Ingestion.Model;

namespace Ingestion.Infrastructure.Messaging
{
    public interface ITelemetryBuffer
    {
        bool TryEnqueue(BufferItem<BessTelemetry> item);
        ValueTask<BufferItem<BessTelemetry>> DequeueAsync(CancellationToken cancellationToken);
        bool TryDequeue(out BufferItem<BessTelemetry> item);
        void Complete();
    }
}
