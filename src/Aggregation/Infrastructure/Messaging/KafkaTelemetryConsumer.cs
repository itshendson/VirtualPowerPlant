using System.Text.Json;
using Akka.Actor;
using Akka.Hosting;
using Aggregation.Actors;
using Aggregation.Infrastructure.Configuration;
using Aggregation.Messages;
using Aggregation.Model;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aggregation.Infrastructure.Messaging;

public sealed class KafkaTelemetryConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<KafkaTelemetryConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly KafkaTopicOptions _topics;
    private readonly ActorRegistry _actorRegistry;

    public KafkaTelemetryConsumer(
        ILogger<KafkaTelemetryConsumer> logger,
        IOptions<KafkaOptions> options,
        ConsumerConfig consumerConfig,
        ActorRegistry actorRegistry)
    {
        _logger = logger;
        _consumerConfig = consumerConfig;
        _topics = options.Value.Topics;
        _actorRegistry = actorRegistry;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ingress = _actorRegistry.Get<TelemetryIngressActor>();

        using var consumer = new ConsumerBuilder<string, byte[]>(_consumerConfig).Build();
        consumer.Subscribe(_topics.TelemetryRaw);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]> result;

                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    continue;
                }

                if (result?.Message?.Value is null)
                {
                    continue;
                }

                if (!TryDeserialize(result.Message.Value, out var telemetry))
                {
                    _logger.LogWarning("Skipping telemetry message with invalid payload. Offset={Offset}", result.Offset);
                    consumer.Commit(result);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(telemetry.SiteId))
                {
                    _logger.LogWarning("Skipping telemetry message missing SiteId. Offset={Offset}", result.Offset);
                    consumer.Commit(result);
                    continue;
                }

                var envelope = new SiteTelemetryReceived(
                    telemetry.SiteId,
                    telemetry,
                    DateTimeOffset.UtcNow);

                ingress.Tell(envelope, ActorRefs.NoSender);
                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            consumer.Close();
        }

        return Task.CompletedTask;
    }

    private static bool TryDeserialize(byte[] payload, out BessTelemetry telemetry)
    {
        telemetry = JsonSerializer.Deserialize<BessTelemetry>(payload, SerializerOptions) ?? new BessTelemetry();
        return !string.IsNullOrWhiteSpace(telemetry.SiteId);
    }
}
