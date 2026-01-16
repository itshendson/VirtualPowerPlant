using System.ComponentModel.DataAnnotations;

namespace Ingestion.Model
{
    /// <summary>
    /// Represents a single telemetry reading of a Battery Energy Storage System (BESS) sent by an edge gateway.
    /// This is the ingestion boundary model.
    /// </summary>
    public sealed class BessTelemetry
    {
        /// <summary>
        /// Unique identifier of the device.
        /// Used as the Kafka message key.
        /// </summary>
        [Required(ErrorMessage = "DeviceId is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "DeviceId must be between 1 and 100 characters")]
        public string DeviceId { get; init; } = string.Empty;

        /// <summary>
        /// Identifier for the site that owns the powerwall.
        /// </summary>
        [Required(ErrorMessage = "SiteId is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "SiteId must be between 1 and 100 characters")]
        public string SiteId { get; init; } = string.Empty;

        /// <summary>
        /// Timestamp when the reading was taken on the device.
        /// Should be UTC.
        /// </summary>
        [Required(ErrorMessage = "Timestamp is required")]
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// State of charge percentage (0-100).
        /// </summary>
        [Range(0, 100, ErrorMessage = "StateOfChargePercentage must be between 0 and 100")]
        public double StateOfChargePercentage { get; init; }

        /// <summary>
        /// Current battery power in kilowatts.
        /// </summary>
        public double BatteryPowerKw { get; init; }

        /// <summary>
        /// Remaining usable energy in kilowatt-hours.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "UsableEnergyRemainingKWh must be non-negative")]
        public double UsableEnergyRemainingKWh { get; init; }

        /// <summary>
        /// Indicates whether the device is currently online.
        /// </summary>
        public bool IsOnline { get; init; }

        /// <summary>
        /// Reported battery temperature in Celsius.
        /// </summary>
        public double BatteryTemperatureC { get; init; }
    }
}
