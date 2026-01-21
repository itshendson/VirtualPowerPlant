namespace Aggregation.Infrastructure;

public sealed class HierarchyOptions
{
    public string DefaultSubstationId { get; init; } = "unknown-substation";
    public string DefaultRegionId { get; init; } = "unknown-region";
    public Dictionary<string, string> SiteToSubstation { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> SubstationToRegion { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}
