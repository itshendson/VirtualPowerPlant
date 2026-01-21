using Confluent.Kafka;
using MetricsDashboard.Infrastructure.Configuration;
using MetricsDashboard.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace MetricsDashboard.Extensions;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));

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

        services.AddHostedService<KafkaAggregateConsumer>();

        return services;
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
