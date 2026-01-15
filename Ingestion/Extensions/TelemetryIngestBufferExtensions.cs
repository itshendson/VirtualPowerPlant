using Ingestion.Infrastructure.Configuration;
using Ingestion.Infrastructure.Messaging;

namespace Ingestion.Extensions
{
    public static class TelemetryIngestBufferExtensions
    {
        public static IServiceCollection AddTelemetryIngestBuffer(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<TelemetryIngestBufferOptions>(configuration.GetSection("IngestBuffer"));
            services.AddSingleton<ITelemetryBuffer, TelemetryBuffer>();
            return services;
        }

        public static IServiceCollection AddTelemetryIngestBackgroundService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHostedService<TelemetryIngestBackgroundService>();
            return services;
        }
    }
}
