using Confluent.Kafka;
using Ingestion.Infrastructure.Configuration;
using Ingestion.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace Ingestion.Extensions
{
    public static class KafkaExtensions
    {
        public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<KafkaOptions>>().Value;
                var optionsProducer = options.Producer;

                return new ProducerConfig
                {
                    BootstrapServers = options.BootstrapServers,
                    ClientId = options.ClientId,
                    EnableIdempotence = optionsProducer.EnableIdempotence,
                    Acks = ParseAcks(optionsProducer.Acks),
                    LingerMs = optionsProducer.LingerMs,
                    BatchSize = optionsProducer.BatchSize,
                    MessageTimeoutMs = optionsProducer.MessageTimeoutMs
                };
            });

            services.AddSingleton<ITelemetryProducer, TelemetryProducer>();

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

    }
}
