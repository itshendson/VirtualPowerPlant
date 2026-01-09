using Ingestion.Infrastructure.Configuration;
using Ingestion.Model;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Ingestion.Infrastructure.Messaging
{
    public class TelemetryIngestBuffer : ITelemetryIngestBuffer
    {
        private readonly Channel<BufferItem<TelemetryReadingRequest>> _channel;

        public TelemetryIngestBuffer(IOptions<TelemetryIngestBufferOptions> options)
        {
            var channelOptions = new BoundedChannelOptions(options.Value.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<BufferItem<TelemetryReadingRequest>>(channelOptions);
        }

        public bool TryEnqueue(BufferItem<TelemetryReadingRequest> item)
        {
            return _channel.Writer.TryWrite(item);
        }

        public ValueTask EnqueueAsync(BufferItem<TelemetryReadingRequest> item, CancellationToken cancellationToken)
        {
            return _channel.Writer.WriteAsync(item, cancellationToken);
        }

        public ValueTask<BufferItem<TelemetryReadingRequest>> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
