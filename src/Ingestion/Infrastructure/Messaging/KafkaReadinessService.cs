using Confluent.Kafka;
using Ingestion.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ingestion.Infrastructure.Messaging
{
    public class KafkaReadinessService : IHostedService
    {
        private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan PostReadyDelay = TimeSpan.FromSeconds(5);

        private readonly ILogger<KafkaReadinessService> _logger;
        private readonly KafkaOptions _options;

        public KafkaReadinessService(ILogger<KafkaReadinessService> logger, IOptions<KafkaOptions> options)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _options.BootstrapServers,
                ClientId = _options.ClientId
            };

            using var admin = new AdminClientBuilder(config).Build();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var metadata = admin.GetMetadata(MetadataTimeout);
                    if (metadata.Brokers.Count > 0)
                    {
                        _logger.LogInformation("Kafka broker metadata available. Waiting briefly before starting producers.");
                        await Task.Delay(PostReadyDelay, cancellationToken);
                        return;
                    }

                    _logger.LogWarning("Kafka metadata is empty. Retrying...");
                }
                catch (KafkaException ex)
                {
                    _logger.LogWarning(ex, "Kafka not ready yet. Retrying...");
                }

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
