namespace Aggregation.Model;

public readonly record struct AggregateMetrics(
    double AvailableEnergyKwh,
    double AvailableDischargeKw,
    double AvailableChargeKw,
    int OnlineCount,
    int OfflineCount,
    double ConfidenceWeightedEnergyKwh)
{
    public static AggregateMetrics Empty => new(0, 0, 0, 0, 0, 0);

    public AggregateMetrics Add(AggregateMetrics other)
    {
        return new AggregateMetrics(
            AvailableEnergyKwh + other.AvailableEnergyKwh,
            AvailableDischargeKw + other.AvailableDischargeKw,
            AvailableChargeKw + other.AvailableChargeKw,
            OnlineCount + other.OnlineCount,
            OfflineCount + other.OfflineCount,
            ConfidenceWeightedEnergyKwh + other.ConfidenceWeightedEnergyKwh);
    }

    public AggregateMetrics Subtract(AggregateMetrics other)
    {
        return new AggregateMetrics(
            AvailableEnergyKwh - other.AvailableEnergyKwh,
            AvailableDischargeKw - other.AvailableDischargeKw,
            AvailableChargeKw - other.AvailableChargeKw,
            OnlineCount - other.OnlineCount,
            OfflineCount - other.OfflineCount,
            ConfidenceWeightedEnergyKwh - other.ConfidenceWeightedEnergyKwh);
    }
}
