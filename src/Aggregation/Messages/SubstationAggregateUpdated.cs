using Aggregation.Model;

namespace Aggregation.Messages;

public sealed record SubstationAggregateUpdated(string RegionId, string SubstationId, AggregateMetrics Metrics) : IEntityMessage
{
    public string EntityId => RegionId;
}
