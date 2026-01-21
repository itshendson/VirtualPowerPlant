using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Confluent.Kafka;
using MetricsDashboard.Infrastructure.Configuration;
using MetricsDashboard.Model;
using Microsoft.Extensions.Options;

namespace MetricsDashboard.Infrastructure.Messaging;

public sealed class KafkaAggregateConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<KafkaAggregateConsumer> _logger;
    private readonly ConsumerConfig _consumerConfig;
    private readonly KafkaTopicOptions _topics;
    private readonly IAggregateSnapshotStore _store;

    public KafkaAggregateConsumer(
        ILogger<KafkaAggregateConsumer> logger,
        IOptions<KafkaOptions> options,
        ConsumerConfig consumerConfig,
        IAggregateSnapshotStore store)
    {
        _logger = logger;
        _consumerConfig = consumerConfig;
        _topics = options.Value.Topics;
        _store = store;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, byte[]>(_consumerConfig).Build();
        consumer.Subscribe(new[]
        {
            _topics.AggregatesSite,
            _topics.AggregatesSubstation,
            _topics.AggregatesRegion
        });

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

                if (!TryHandleMessage(result.Topic, result.Message.Value))
                {
                    _logger.LogWarning("Skipping aggregate with invalid payload. Topic={Topic} Offset={Offset}", result.Topic, result.Offset);
                }

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

    private bool TryHandleMessage(string topic, byte[] payload)
    {
        if (string.Equals(topic, _topics.AggregatesSite, StringComparison.OrdinalIgnoreCase))
        {
            if (!TryDeserialize(payload, out SiteAggregate? aggregate) || aggregate is null)
            {
                return false;
            }

            _store.UpdateSite(aggregate);
            return true;
        }

        if (string.Equals(topic, _topics.AggregatesSubstation, StringComparison.OrdinalIgnoreCase))
        {
            if (!TryDeserialize(payload, out SubstationAggregate? aggregate) || aggregate is null)
            {
                return false;
            }

            _store.UpdateSubstation(aggregate);
            return true;
        }

        if (string.Equals(topic, _topics.AggregatesRegion, StringComparison.OrdinalIgnoreCase))
        {
            if (!TryDeserialize(payload, out RegionAggregate? aggregate) || aggregate is null)
            {
                return false;
            }

            _store.UpdateRegion(aggregate);
            return true;
        }

        return false;
    }

    private static bool TryDeserialize<T>(byte[] payload, [NotNullWhen(true)] out T? aggregate)
        where T : class
    {
        aggregate = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        return aggregate is not null;
    }
}
