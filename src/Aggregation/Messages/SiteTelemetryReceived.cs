using Aggregation.Model;

namespace Aggregation.Messages;

public sealed record SiteTelemetryReceived(string SiteId, BessTelemetry Telemetry, DateTimeOffset ReceivedAt)
    : IEntityMessage
{
    public string EntityId => SiteId;
}
