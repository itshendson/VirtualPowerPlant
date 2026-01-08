using Confluent.Kafka;
using Ingestion.Infrastructure.Configuration;
using Ingestion.Infrastructure.Messaging;
using Ingestion.Model;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text;

namespace Ingestion.Handlers
{
    public record IngestTelemetryCommand(TelemetryReadingRequest reading) : IRequest<CommandResult>;

    public class IngestTelemetryCommandHandler : IRequestHandler<IngestTelemetryCommand, CommandResult>
    {
        private readonly ILogger<IngestTelemetryCommandHandler> _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly ITelemetryProducer _producer;

        public IngestTelemetryCommandHandler(ILogger<IngestTelemetryCommandHandler> logger, IOptions<KafkaOptions> kafkaOptions, ITelemetryProducer producer)
        {
            _logger = logger;
            _producer = producer;
            _kafkaOptions = kafkaOptions.Value;
        }

        public async Task<CommandResult> Handle(IngestTelemetryCommand request, CancellationToken cancellationToken)
        {
            var eventId = Guid.NewGuid().ToString("N");

            try
            {
                var headers = new Headers
                {
                    { "eventId", Encoding.ASCII.GetBytes(eventId) }
                };

                await _producer.ProduceAsync<string, TelemetryReadingRequest>(
                    topic: _kafkaOptions.Topics.TelemetryRaw,
                    key: request.reading.MeterId,
                    value: request.reading,
                    headers: headers,
                    cancellationToken: cancellationToken);

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
