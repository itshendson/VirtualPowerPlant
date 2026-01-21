using Aggregation.Infrastructure;
using Aggregation.Infrastructure.Configuration;
using Aggregation.Infrastructure.Messaging;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Aggregation.Extensions;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var producerOptions = options.Producer;

            return new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                ClientId = options.ClientId,
                EnableIdempotence = producerOptions.EnableIdempotence,
                Acks = ParseAcks(producerOptions.Acks),
                LingerMs = producerOptions.LingerMs,
                BatchSize = producerOptions.BatchSize,
                MessageTimeoutMs = producerOptions.MessageTimeoutMs
            };
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
            var consumerOptions = options.Consumer;

            return new ConsumerConfig
            {
                BootstrapServers = options.BootstrapServers,
                ClientId = options.ClientId,
                GroupId = consumerOptions.GroupId,
                AutoOffsetReset = ParseOffsetReset(consumerOptions.AutoOffsetReset),
                EnableAutoCommit = consumerOptions.EnableAutoCommit,
                MaxPollIntervalMs = consumerOptions.MaxPollIntervalMs
            };
        });

        services.AddSingleton<IAggregatePublisher, KafkaAggregatePublisher>();
        services.AddHostedService<KafkaTelemetryConsumer>();

        return services;
    }

    private static Acks ParseAcks(string? acks)
    {
        return acks?.ToLower() switch
        {
            "all" => Acks.All,
            "leader" => Acks.Leader,
            "none" => Acks.None,
            _ => Acks.All
        };
    }

    private static AutoOffsetReset ParseOffsetReset(string? reset)
    {
        return reset?.ToLower() switch
        {
            "earliest" => AutoOffsetReset.Earliest,
            "latest" => AutoOffsetReset.Latest,
            "error" => AutoOffsetReset.Error,
            _ => AutoOffsetReset.Earliest
        };
    }
}
