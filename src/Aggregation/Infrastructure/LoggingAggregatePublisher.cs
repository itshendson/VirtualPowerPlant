using Aggregation.Model;

namespace Aggregation.Infrastructure;

public sealed class LoggingAggregatePublisher(ILogger<LoggingAggregatePublisher> logger) : IAggregatePublisher
{
    public Task PublishSiteAggregateAsync(SiteAggregate aggregate, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Site {SiteId} aggregates updated: Substation={SubstationId} Energy={EnergyKwh}kWh Discharge={DischargeKw}kW Charge={ChargeKw}kW Online={Online} Offline={Offline}",
            aggregate.SiteId,
            aggregate.SubstationId,
            aggregate.Metrics.AvailableEnergyKwh,
            aggregate.Metrics.AvailableDischargeKw,
            aggregate.Metrics.AvailableChargeKw,
            aggregate.Metrics.OnlineCount,
            aggregate.Metrics.OfflineCount);

        return Task.CompletedTask;
    }

    public Task PublishSubstationAggregateAsync(SubstationAggregate aggregate, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Substation {SubstationId} aggregates updated: Region={RegionId} Energy={EnergyKwh}kWh Discharge={DischargeKw}kW Charge={ChargeKw}kW Online={Online} Offline={Offline}",
            aggregate.SubstationId,
            aggregate.RegionId,
            aggregate.Metrics.AvailableEnergyKwh,
            aggregate.Metrics.AvailableDischargeKw,
            aggregate.Metrics.AvailableChargeKw,
            aggregate.Metrics.OnlineCount,
            aggregate.Metrics.OfflineCount);

        return Task.CompletedTask;
    }

    public Task PublishRegionAggregateAsync(RegionAggregate aggregate, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Region {RegionId} aggregates updated: Energy={EnergyKwh}kWh Discharge={DischargeKw}kW Charge={ChargeKw}kW Online={Online} Offline={Offline}",
            aggregate.RegionId,
            aggregate.Metrics.AvailableEnergyKwh,
            aggregate.Metrics.AvailableDischargeKw,
            aggregate.Metrics.AvailableChargeKw,
            aggregate.Metrics.OnlineCount,
            aggregate.Metrics.OfflineCount);

        return Task.CompletedTask;
    }
}
