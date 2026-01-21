namespace Aggregation.Infrastructure;

public interface IHierarchyResolver
{
    string ResolveSubstationId(string siteId);
    string ResolveRegionId(string substationId);
}
