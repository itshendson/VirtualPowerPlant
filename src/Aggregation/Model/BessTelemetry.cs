namespace Aggregation.Model;

public sealed record BessTelemetry(
    DateTimeOffset Timestamp,
    double StateOfChargePercent,
    double UsableEnergyKwh,
    double AvailableDischargePowerKw,
    double AvailableChargePowerKw,
    bool IsOnline,
    double Confidence = 1.0);
