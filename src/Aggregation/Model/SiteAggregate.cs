namespace Aggregation.Model;

public sealed record SiteAggregate(string SiteId, string SubstationId, AggregateMetrics Metrics, DateTimeOffset UpdatedAt);
