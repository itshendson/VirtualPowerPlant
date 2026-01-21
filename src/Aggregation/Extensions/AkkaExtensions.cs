using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Hosting;
using Aggregation.Actors;
using Aggregation.Infrastructure;
using Aggregation.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aggregation.Extensions;

public static class AkkaExtensions
{
    public static IServiceCollection AddAkkaService(this IServiceCollection services)
    {
        services.AddOptions<HierarchyOptions>().BindConfiguration("Aggregation:Hierarchy");
        services.AddSingleton<IHierarchyResolver, InMemoryHierarchyResolver>();

        services.AddAkka("aggregation-actor-system", (akkaBuilder, sp) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var akkaConfig = configuration.GetSection("akka");
            if (akkaConfig.Exists())
            {
                akkaBuilder.AddHocon(akkaConfig, HoconAddMode.Prepend);
            }

            akkaBuilder.WithActors((system, registry) =>
            {
                var shardCount = configuration.GetValue("Aggregation:Sharding:ShardCount", 100);
                var messageExtractor = new EntityMessageExtractor(shardCount);
                var sharding = ClusterSharding.Get(system);
                var shardingSettings = sharding.Settings.WithRole(ActorNames.ClusterRole);

                var hierarchyResolver = sp.GetRequiredService<IHierarchyResolver>();
                var publisher = sp.GetRequiredService<IAggregatePublisher>();
                var publisherActor = system.ActorOf(
                    Props.Create(() => new AggregatePublisherActor(publisher)),
                    ActorNames.AggregatePublisher);

                var regionShard = sharding.Start(
                    ActorNames.RegionShard,
                    entityId => RegionActor.Props(entityId, publisherActor),
                    shardingSettings,
                    messageExtractor);

                var substationShard = sharding.Start(
                    ActorNames.SubstationShard,
                    entityId => SubstationActor.Props(entityId, hierarchyResolver, regionShard, publisherActor),
                    shardingSettings,
                    messageExtractor);

                var siteShard = sharding.Start(
                    ActorNames.SiteShard,
                    entityId => SiteActor.Props(entityId, hierarchyResolver, substationShard, publisherActor),
                    shardingSettings,
                    messageExtractor);

                var ingress = system.ActorOf(
                    Props.Create(() => new TelemetryIngressActor(siteShard)),
                    ActorNames.TelemetryIngress);

                registry.TryRegister<TelemetryIngressActor>(ingress);
            });
        });
        services.AddHealthChecks().AddCheck<AkkaClusterHealthCheck>("akka.cluster");

        return services;
    }
}
