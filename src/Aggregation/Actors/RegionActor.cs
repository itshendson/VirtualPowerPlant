using Akka.Actor;
using Aggregation.Messages;
using Aggregation.Model;

namespace Aggregation.Actors;

public sealed class RegionActor : ReceiveActor
{
    private readonly string _regionId;
    private readonly IActorRef _publisher;
    private readonly Dictionary<string, AggregateMetrics> _substationMetrics = new(StringComparer.OrdinalIgnoreCase);
    private AggregateMetrics _total = AggregateMetrics.Empty;

    public RegionActor(string regionId, IActorRef publisher)
    {
        _regionId = regionId;
        _publisher = publisher;

        Receive<SubstationAggregateUpdated>(HandleSubstationAggregate);
    }

    private void HandleSubstationAggregate(SubstationAggregateUpdated message)
    {
        if (!string.Equals(message.RegionId, _regionId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_substationMetrics.TryGetValue(message.SubstationId, out var previous))
        {
            _total = _total.Subtract(previous);
        }

        _substationMetrics[message.SubstationId] = message.Metrics;
        _total = _total.Add(message.Metrics);

        var aggregate = new RegionAggregate(_regionId, _total, DateTimeOffset.UtcNow);
        _publisher.Tell(aggregate);
    }

    public static Props Props(string regionId, IActorRef publisher)
    {
        return Akka.Actor.Props.Create(() => new RegionActor(regionId, publisher));
    }
}
