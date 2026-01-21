using System.Text.Json;
using Aggregation.Infrastructure.Configuration;
using Aggregation.Model;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Aggregation.Infrastructure.Messaging;

public sealed class KafkaAggregatePublisher : IAggregatePublisher, IAsyncDisposable, IDisposable
{
    private readonly ILogger<KafkaAggregatePublisher> _logger;
    private readonly IProducer<string, byte[]> _producer;
    private readonly KafkaTopicOptions _topics;
    private int _disposed;

    public KafkaAggregatePublisher(
        ILogger<KafkaAggregatePublisher> logger,
        IOptions<KafkaOptions> options,
        ProducerConfig config)
    {
        _logger = logger;
        _topics = options.Value.Topics;
        _producer = new ProducerBuilder<string, byte[]>(config).Build();
    }

    public async Task PublishRegionAggregateAsync(RegionAggregate aggregate, CancellationToken cancellationToken)
    {
        await PublishAsync(
                _topics.AggregatesRegion,
                aggregate.RegionId,
                aggregate,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task PublishSiteAggregateAsync(SiteAggregate aggregate, CancellationToken cancellationToken)
    {
        await PublishAsync(
                _topics.AggregatesSite,
                aggregate.SiteId,
                aggregate,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task PublishSubstationAggregateAsync(SubstationAggregate aggregate, CancellationToken cancellationToken)
    {
        await PublishAsync(
                _topics.AggregatesSubstation,
                aggregate.SubstationId,
                aggregate,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task PublishAsync<T>(
        string topic,
        string key,
        T payload,
        CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);

        try
        {
            await _producer.ProduceAsync(
                    topic,
                    new Message<string, byte[]>
                    {
                        Key = key,
                        Value = bytes
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish aggregate to {Topic} with key {Key}.", topic, key);
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while flushing Kafka producer.");
        }
        finally
        {
            _producer.Dispose();
        }

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while flushing Kafka producer during disposal.");
        }
        finally
        {
            _producer.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
