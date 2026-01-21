using Aggregation.Model;

namespace Aggregation.Infrastructure;

public interface IAggregatePublisher
{
    Task PublishSiteAggregateAsync(SiteAggregate aggregate, CancellationToken cancellationToken);

    Task PublishSubstationAggregateAsync(SubstationAggregate aggregate, CancellationToken cancellationToken);

    Task PublishRegionAggregateAsync(RegionAggregate aggregate, CancellationToken cancellationToken);
}
