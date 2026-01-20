using Akka.Actor;
using Akka.Cluster;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aggregation.Extensions;

public sealed class AkkaClusterHealthCheck : IHealthCheck
{
    private readonly ActorSystem _actorSystem;

    public AkkaClusterHealthCheck(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cluster = Cluster.Get(_actorSystem);
        var status = cluster.SelfMember.Status;

        return Task.FromResult(status switch
        {
            MemberStatus.Up => HealthCheckResult.Healthy("Cluster member is Up."),
            MemberStatus.Joining => HealthCheckResult.Degraded($"Cluster member status: {status}."),
            _ => HealthCheckResult.Unhealthy($"Cluster member status: {status}.")
        });
    }
}
