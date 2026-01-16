using Ingestion.Infrastructure.Configuration;
using Ingestion.Infrastructure.Messaging;
using Ingestion.Model;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Ingestion.Handlers
{
    public record IngestTelemetryCommand(BessTelemetry reading) : IRequest<CommandResult>;

    public class IngestTelemetryCommandHandler : IRequestHandler<IngestTelemetryCommand, CommandResult>
    {
        private readonly ILogger<IngestTelemetryCommandHandler> _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly ITelemetryBuffer _buffer;

        public IngestTelemetryCommandHandler(ILogger<IngestTelemetryCommandHandler> logger, IOptions<KafkaOptions> kafkaOptions, ITelemetryBuffer buffer)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions.Value;
            _buffer = buffer;
        }

        public Task<CommandResult> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
        {
            var eventId = Guid.NewGuid().ToString("N");

            try
            {
                var item = new BufferItem<BessTelemetry>(
                    Topic: _kafkaOptions.Topics.TelemetryRaw,
                    Key: request.reading.DeviceId,
                    Value: request.reading,
                    EventId: eventId);

                if (!_buffer.TryEnqueue(item))
                {
                    _logger.LogWarning("Telemetry buffer full. Rejecting ingest request. EventId: {EventId}, DeviceId: {DeviceId}", eventId, request.reading.DeviceId);
                    return Task.FromResult(CommandResult.Failure(StatusCodes.Status503ServiceUnavailable, "Telemetry ingestion buffer is full"));
                }

                _logger.LogDebug("Successfully ingested telemetry event. EventId: {EventId}, DeviceId: {DeviceId}", eventId, request.reading.DeviceId);
                return Task.FromResult(CommandResult.Success());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest telemetry event. EventId: {EventId}, DeviceId: {DeviceId}", eventId, request.reading.DeviceId);
                return Task.FromResult(CommandResult.Failure("Failed to ingest telemetry reading"));
            }
        }
    }
}
