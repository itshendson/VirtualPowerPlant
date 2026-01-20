using Akka.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aggregation.Extensions;

public static class AkkaExtensions
{
    public static IServiceCollection AddAkkaService(this IServiceCollection services)
    {
        services.AddAkka("aggregation-actor-system", (akkaBuilder, sp) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var akkaConfig = configuration.GetSection("akka");
            if (akkaConfig.Exists())
            {
                akkaBuilder.AddHocon(akkaConfig, HoconAddMode.Prepend);
            }
        });
        services.AddHealthChecks().AddCheck<AkkaClusterHealthCheck>("akka.cluster");

        return services;
    }
}
