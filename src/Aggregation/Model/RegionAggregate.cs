namespace Aggregation.Model;

public sealed record RegionAggregate(string RegionId, AggregateMetrics Metrics, DateTimeOffset UpdatedAt);
