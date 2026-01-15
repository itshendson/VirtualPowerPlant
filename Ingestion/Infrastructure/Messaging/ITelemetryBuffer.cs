using Ingestion.Model;

namespace Ingestion.Infrastructure.Messaging
{
    public interface ITelemetryBuffer
    {
        bool TryEnqueue(BufferItem<Telemetry> item);
        ValueTask<BufferItem<Telemetry>> DequeueAsync(CancellationToken cancellationToken);
        bool TryDequeue(out BufferItem<Telemetry> item);
        void Complete();
    }
}
