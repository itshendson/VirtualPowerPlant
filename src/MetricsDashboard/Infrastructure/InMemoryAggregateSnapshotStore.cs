using System.Collections.Concurrent;
using MetricsDashboard.Model;

namespace MetricsDashboard.Infrastructure;

public sealed class InMemoryAggregateSnapshotStore : IAggregateSnapshotStore
{
    private readonly ConcurrentDictionary<string, SiteAggregate> _sites = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SubstationAggregate> _substations = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RegionAggregate> _regions = new(StringComparer.OrdinalIgnoreCase);

    public void UpdateSite(SiteAggregate aggregate)
    {
        _sites[aggregate.SiteId] = aggregate;
    }

    public void UpdateSubstation(SubstationAggregate aggregate)
    {
        _substations[aggregate.SubstationId] = aggregate;
    }

    public void UpdateRegion(RegionAggregate aggregate)
    {
        _regions[aggregate.RegionId] = aggregate;
    }

    public IReadOnlyCollection<SiteAggregate> GetSites()
    {
        return _sites.Values
            .OrderByDescending(site => site.UpdatedAt)
            .ToArray();
    }

    public IReadOnlyCollection<SubstationAggregate> GetSubstations()
    {
        return _substations.Values
            .OrderByDescending(substation => substation.UpdatedAt)
            .ToArray();
    }

    public IReadOnlyCollection<RegionAggregate> GetRegions()
    {
        return _regions.Values
            .OrderByDescending(region => region.UpdatedAt)
            .ToArray();
    }
}
