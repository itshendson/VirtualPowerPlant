using Ingestion.Infrastructure.Configuration;
using Ingestion.Infrastructure.Messaging;
using Ingestion.Model;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Ingestion.Handlers
{
    public record IngestTelemetryCommand(TelemetryReadingRequest reading) : IRequest<CommandResult>;

    public class IngestTelemetryCommandHandler : IRequestHandler<IngestTelemetryCommand, CommandResult>
    {
        private readonly ILogger<IngestTelemetryCommandHandler> _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly ITelemetryIngestBuffer _buffer;

        public IngestTelemetryCommandHandler(ILogger<IngestTelemetryCommandHandler> logger, IOptions<KafkaOptions> kafkaOptions, ITelemetryIngestBuffer buffer)
        {
            _logger = logger;
            _kafkaOptions = kafkaOptions.Value;
            _buffer = buffer;
        }

        public async Task<CommandResult> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
        {
            var eventId = Guid.NewGuid().ToString("N");

            try
            {
                var workItem = new TelemetryIngestionWorkItem(
                    Topic: _kafkaOptions.Topics.TelemetryRaw,
                    Key: request.reading.MeterId,
                    Value: request.reading,
                    EventId: eventId);

                await _buffer.EnqueueAsync(workItem, cancellationToken);

                _logger.LogDebug("Successfully ingested telemetry event. EventId: {EventId}, MeterId: {MeterId}", eventId, request.reading.MeterId);
                return CommandResult.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Request was cancelled by the client. EventId: {EventId}", eventId);
                return CommandResult.Failure(StatusCodes.Status499ClientClosedRequest, "Request was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest telemetry event. EventId: {EventId}, MeterId: {MeterId}", eventId, request.reading.MeterId);
                return CommandResult.Failure("Failed to ingest telemetry reading");
            }
        }
    }
}
