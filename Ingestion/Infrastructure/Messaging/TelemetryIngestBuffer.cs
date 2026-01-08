using Ingestion.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Ingestion.Infrastructure.Messaging
{
    public class TelemetryIngestBuffer : ITelemetryIngestBuffer
    {
        private readonly Channel<TelemetryIngestionWorkItem> _channel;

        public TelemetryIngestBuffer(IOptions<TelemetryIngestBufferOptions> options)
        {
            var channelOptions = new BoundedChannelOptions(options.Value.Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<TelemetryIngestionWorkItem>(channelOptions);
        }

        public bool TryEnqueue(TelemetryIngestionWorkItem item)
        {
            return _channel.Writer.TryWrite(item);
        }

        public ValueTask<TelemetryIngestionWorkItem> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}
