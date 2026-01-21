using Aggregation.Model;

namespace Aggregation.Messages;

public sealed record SiteAggregateUpdated(string SubstationId, string SiteId, AggregateMetrics Metrics) : IEntityMessage
{
    public string EntityId => SubstationId;
}
