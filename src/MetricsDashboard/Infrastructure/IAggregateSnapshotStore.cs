using MetricsDashboard.Model;

namespace MetricsDashboard.Infrastructure;

public interface IAggregateSnapshotStore
{
    void UpdateSite(SiteAggregate aggregate);

    void UpdateSubstation(SubstationAggregate aggregate);

    void UpdateRegion(RegionAggregate aggregate);

    IReadOnlyCollection<SiteAggregate> GetSites();

    IReadOnlyCollection<SubstationAggregate> GetSubstations();

    IReadOnlyCollection<RegionAggregate> GetRegions();
}
