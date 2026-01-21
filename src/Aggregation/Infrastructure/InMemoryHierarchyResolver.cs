using Microsoft.Extensions.Options;

namespace Aggregation.Infrastructure;

public sealed class InMemoryHierarchyResolver : IHierarchyResolver
{
    private readonly HierarchyOptions _options;

    public InMemoryHierarchyResolver(IOptions<HierarchyOptions> options)
    {
        _options = options.Value;
    }

    public string ResolveSubstationId(string siteId)
    {
        if (_options.SiteToSubstation.TryGetValue(siteId, out var substationId))
        {
            return substationId;
        }

        return _options.DefaultSubstationId;
    }

    public string ResolveRegionId(string substationId)
    {
        if (_options.SubstationToRegion.TryGetValue(substationId, out var regionId))
        {
            return regionId;
        }

        return _options.DefaultRegionId;
    }
}
