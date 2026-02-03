using Akka.Actor;
using System.Collections.Generic;
using Aggregation.Infrastructure;
using Aggregation.Messages;
using Aggregation.Model;

namespace Aggregation.Actors;

public sealed class SiteActor : ReceiveActor
{
    private readonly string _siteId;
    private readonly IHierarchyResolver _hierarchyResolver;
    private readonly IActorRef _substationShard;
    private readonly IActorRef _publisher;
    private readonly Dictionary<string, string> _lastEventIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _lastTimestamps = new(StringComparer.Ordinal);
    private SiteState _state = SiteState.Empty;

    public SiteActor(string siteId, IHierarchyResolver hierarchyResolver, IActorRef substationShard, IActorRef publisher)
    {
        _siteId = siteId;
        _hierarchyResolver = hierarchyResolver;
        _substationShard = substationShard;
        _publisher = publisher;

        Receive<SiteTelemetryReceived>(HandleTelemetry);
    }

    private void HandleTelemetry(SiteTelemetryReceived message)
    {
        if (!ShouldProcess(message.Telemetry))
        {
            return;
        }

        var metrics = BuildMetrics(message.Telemetry);
        _state = _state with
        {
            Telemetry = message.Telemetry,
            Metrics = metrics,
            LastUpdated = message.ReceivedAt
        };

        var substationId = _hierarchyResolver.ResolveSubstationId(_siteId);
        _publisher.Tell(new SiteAggregate(_siteId, substationId, metrics, message.ReceivedAt));
        _substationShard.Tell(new SiteAggregateUpdated(substationId, _siteId, metrics));
    }

    private bool ShouldProcess(BessTelemetry telemetry)
    {
        if (string.IsNullOrWhiteSpace(telemetry.DeviceId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(telemetry.EventId)
            && _lastEventIds.TryGetValue(telemetry.DeviceId, out var lastEventId)
            && string.Equals(lastEventId, telemetry.EventId, StringComparison.Ordinal))
        {
            return false;
        }

        if (_lastTimestamps.TryGetValue(telemetry.DeviceId, out var lastTimestamp)
            && telemetry.Timestamp <= lastTimestamp)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(telemetry.EventId))
        {
            _lastEventIds[telemetry.DeviceId] = telemetry.EventId;
        }

        _lastTimestamps[telemetry.DeviceId] = telemetry.Timestamp;
        return true;
    }

    private static AggregateMetrics BuildMetrics(BessTelemetry telemetry)
    {
        if (!telemetry.IsOnline)
        {
            return new AggregateMetrics(0, 0, 0, 0, 1, 0);
        }

        var dischargeKw = Math.Max(0, telemetry.BatteryPowerKw);
        var chargeKw = Math.Max(0, -telemetry.BatteryPowerKw);
        var confidenceWeightedEnergy = telemetry.UsableEnergyRemainingKWh;

        return new AggregateMetrics(
            telemetry.UsableEnergyRemainingKWh,
            dischargeKw,
            chargeKw,
            1,
            0,
            confidenceWeightedEnergy);
    }

    public static Props Props(
        string siteId,
        IHierarchyResolver hierarchyResolver,
        IActorRef substationShard,
        IActorRef publisher)
    {
        return Akka.Actor.Props.Create(() => new SiteActor(siteId, hierarchyResolver, substationShard, publisher));
    }

    private sealed record SiteState(BessTelemetry? Telemetry, AggregateMetrics Metrics, DateTimeOffset? LastUpdated)
    {
        public static SiteState Empty => new(null, AggregateMetrics.Empty, null);
    }
}
