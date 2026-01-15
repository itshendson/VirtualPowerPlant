using Ingestion.Infrastructure.Configuration;
using Ingestion.Model;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Ingestion.Infrastructure.Messaging
{
    public class TelemetryBuffer : ITelemetryBuffer
    {
        private readonly Channel<BufferItem<Telemetry>> _channel;

        public TelemetryBuffer(IOptions<TelemetryIngestBufferOptions> options)
        {
            var channelOptions = new BoundedChannelOptions(options.Value.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<BufferItem<Telemetry>>(channelOptions);
        }

        public bool TryEnqueue(BufferItem<Telemetry> item)
        {
            return _channel.Writer.TryWrite(item);
        }

        public ValueTask<BufferItem<Telemetry>> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }

        public bool TryDequeue(out BufferItem<Telemetry> item)
        {
            return _channel.Reader.TryRead(out item);
        }

        public void Complete()
        {
            _channel.Writer.TryComplete();
        }
    }
}
