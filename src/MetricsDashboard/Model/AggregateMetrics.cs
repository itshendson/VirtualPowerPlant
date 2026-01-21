namespace MetricsDashboard.Model;

public readonly record struct AggregateMetrics(
    double AvailableEnergyKwh,
    double AvailableDischargeKw,
    double AvailableChargeKw,
    int OnlineCount,
    int OfflineCount,
    double ConfidenceWeightedEnergyKwh);
