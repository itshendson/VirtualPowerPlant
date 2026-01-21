namespace Aggregation.Model;

public sealed record SubstationAggregate(
    string SubstationId,
    string RegionId,
    AggregateMetrics Metrics,
    DateTimeOffset UpdatedAt);
