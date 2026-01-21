using Akka.Actor;
using Aggregation.Infrastructure;
using Aggregation.Model;

namespace Aggregation.Actors;

public sealed class AggregatePublisherActor : ReceiveActor
{
    private readonly IAggregatePublisher _publisher;

    public AggregatePublisherActor(IAggregatePublisher publisher)
    {
        _publisher = publisher;

        ReceiveAsync<SiteAggregate>(async aggregate =>
        {
            await _publisher.PublishSiteAggregateAsync(aggregate, CancellationToken.None)
                .ConfigureAwait(false);
        });

        ReceiveAsync<SubstationAggregate>(async aggregate =>
        {
            await _publisher.PublishSubstationAggregateAsync(aggregate, CancellationToken.None)
                .ConfigureAwait(false);
        });

        ReceiveAsync<RegionAggregate>(async aggregate =>
        {
            await _publisher.PublishRegionAggregateAsync(aggregate, CancellationToken.None)
                .ConfigureAwait(false);
        });
    }
}
