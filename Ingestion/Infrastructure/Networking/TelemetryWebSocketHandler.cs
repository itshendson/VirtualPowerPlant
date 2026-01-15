using System.Buffers;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.WebSockets;
using Google.Protobuf.WellKnownTypes;
using Ingestion.Handlers;
using Ingestion.Model;
using Ingestion.Protos;
using MediatR;

namespace Ingestion.Infrastructure.Networking
{
    public sealed class TelemetryWebSocketHandler
    {
        private const int ReceiveBufferBytes = 4 * 1024;
        private const int MaxMessageBytes = 64 * 1024;

        private readonly ILogger<TelemetryWebSocketHandler> _logger;
        private readonly IMediator _mediator;

        public TelemetryWebSocketHandler(ILogger<TelemetryWebSocketHandler> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public async Task HandleAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferBytes);

            try
            {
                while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
                {
                    var reading = await ReceiveTelemetryAsync(socket, buffer, cancellationToken);
                    if (reading is null)
                    {
                        return;
                    }

                    if (!TryValidate(reading, out var errors))
                    {
                        _logger.LogWarning("Telemetry payload failed validation. Errors: {Errors}", string.Join("; ", errors));
                        await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "Telemetry validation failed");
                        return;
                    }

                    var result = await _mediator.Send(new IngestTelemetryCommand(reading), cancellationToken);
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning("Telemetry ingest rejected. Status: {Status} Error: {Error}", result.HttpStatusCode, result.ErrorMessage);
                        await CloseAsync(socket, WebSocketCloseStatus.EndpointUnavailable, "Telemetry ingest rejected");
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocket error while receiving telemetry.");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task<Telemetry?> ReceiveTelemetryAsync(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
        {
            using var payload = new MemoryStream();

            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.NormalClosure, "Closing");
                    return null;
                }

                if (result.MessageType != WebSocketMessageType.Binary)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.InvalidMessageType, "Binary frames only");
                    return null;
                }

                if (payload.Length + result.Count > MaxMessageBytes)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.MessageTooBig, "Telemetry payload too large");
                    return null;
                }

                payload.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            payload.Position = 0;

            try
            {
                var reading = TelemetryReading.Parser.ParseFrom(payload);
                if (!TryMapReading(reading, out var mapped, out var error))
                {
                    _logger.LogWarning("Telemetry payload failed validation. Error: {Error}", error);
                    await CloseAsync(socket, WebSocketCloseStatus.InvalidPayloadData, "Invalid telemetry payload");
                    return null;
                }

                return mapped;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decode telemetry payload.");
                await CloseAsync(socket, WebSocketCloseStatus.InvalidPayloadData, "Invalid telemetry payload");
                return null;
            }
        }

        private static bool TryMapReading(TelemetryReading reading, out Telemetry mapped, out string error)
        {
            if (reading.ReadingTime is null)
            {
                mapped = null!;
                error = "ReadingTime is required";
                return false;
            }

            if (double.IsNaN(reading.Kw) || double.IsInfinity(reading.Kw))
            {
                mapped = null!;
                error = "Kw must be a finite number";
                return false;
            }

            if (double.IsNaN(reading.StateOfChargePercent) || double.IsInfinity(reading.StateOfChargePercent))
            {
                mapped = null!;
                error = "StateOfChargePercent must be a finite number";
                return false;
            }

            if (reading.HasTemperatureC && (double.IsNaN(reading.TemperatureC) || double.IsInfinity(reading.TemperatureC)))
            {
                mapped = null!;
                error = "TemperatureC must be a finite number";
                return false;
            }

            decimal kw;
            decimal stateOfChargePercent;
            decimal? temperatureC = null;

            try
            {
                kw = Convert.ToDecimal(reading.Kw);
                stateOfChargePercent = Convert.ToDecimal(reading.StateOfChargePercent);

                if (reading.HasTemperatureC)
                {
                    temperatureC = Convert.ToDecimal(reading.TemperatureC);
                }
            }
            catch (Exception)
            {
                mapped = null!;
                error = "Telemetry numeric value is out of range";
                return false;
            }

            mapped = new Telemetry
            {
                MeterId = reading.MeterId,
                ReadingTimeUtc = ToDateTimeOffset(reading.ReadingTime),
                Kw = kw,
                StateOfChargePercent = stateOfChargePercent,
                Status = reading.Status,
                TemperatureC = temperatureC,
                GatewayId = reading.GatewayId,
                FirmwareVersion = reading.FirmwareVersion
            };
            error = string.Empty;
            return true;
        }

        private static DateTimeOffset ToDateTimeOffset(Timestamp timestamp)
        {
            var utc = timestamp.ToDateTime();
            if (utc.Kind != DateTimeKind.Utc)
            {
                utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            }

            return new DateTimeOffset(utc);
        }

        private static bool TryValidate(Telemetry reading, out IReadOnlyList<string> errors)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(reading);
            var isValid = Validator.TryValidateObject(reading, context, validationResults, validateAllProperties: true);

            if (isValid)
            {
                errors = Array.Empty<string>();
                return true;
            }

            errors = validationResults
                .Select(result => result.ErrorMessage ?? "Validation failed")
                .ToArray();

            return false;
        }

        private static Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string description)
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                return socket.CloseAsync(status, description, CancellationToken.None);
            }

            return Task.CompletedTask;
        }
    }
}
