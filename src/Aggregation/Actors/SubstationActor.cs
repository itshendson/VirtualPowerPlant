using Akka.Actor;
using Aggregation.Infrastructure;
using Aggregation.Messages;
using Aggregation.Model;

namespace Aggregation.Actors;

public sealed class SubstationActor : ReceiveActor
{
    private readonly string _substationId;
    private readonly IHierarchyResolver _hierarchyResolver;
    private readonly IActorRef _regionShard;
    private readonly IActorRef _publisher;
    private readonly Dictionary<string, AggregateMetrics> _siteMetrics = new(StringComparer.OrdinalIgnoreCase);
    private AggregateMetrics _total = AggregateMetrics.Empty;

    public SubstationActor(
        string substationId,
        IHierarchyResolver hierarchyResolver,
        IActorRef regionShard,
        IActorRef publisher)
    {
        _substationId = substationId;
        _hierarchyResolver = hierarchyResolver;
        _regionShard = regionShard;
        _publisher = publisher;

        Receive<SiteAggregateUpdated>(HandleSiteAggregate);
    }

    private void HandleSiteAggregate(SiteAggregateUpdated message)
    {
        if (!string.Equals(message.SubstationId, _substationId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_siteMetrics.TryGetValue(message.SiteId, out var previous))
        {
            _total = _total.Subtract(previous);
        }

        _siteMetrics[message.SiteId] = message.Metrics;
        _total = _total.Add(message.Metrics);

        var regionId = _hierarchyResolver.ResolveRegionId(_substationId);
        _publisher.Tell(new SubstationAggregate(_substationId, regionId, _total, DateTimeOffset.UtcNow));
        _regionShard.Tell(new SubstationAggregateUpdated(regionId, _substationId, _total));
    }

    public static Props Props(
        string substationId,
        IHierarchyResolver hierarchyResolver,
        IActorRef regionShard,
        IActorRef publisher)
    {
        return Akka.Actor.Props.Create(() => new SubstationActor(substationId, hierarchyResolver, regionShard, publisher));
    }
}
