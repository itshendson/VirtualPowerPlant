using System.ComponentModel.DataAnnotations;

namespace Ingestion.Model
{
    /// <summary>
    /// Represents a single telemetry reading sent by an edge gateway.
    /// This is the ingestion boundary model.
    /// </summary>
    public sealed class Telemetry
    {
        /// <summary>
        /// Unique identifier of the meter or device.
        /// Used as the Kafka message key.
        /// </summary>
        [Required(ErrorMessage = "MeterId is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "MeterId must be between 1 and 100 characters")]
        public string MeterId { get; init; } = string.Empty;

        /// <summary>
        /// Timestamp when the reading was taken on the device.
        /// Should be UTC.
        /// </summary>
        [Required(ErrorMessage = "ReadingTime is required")]
        public DateTimeOffset ReadingTimeUtc { get; init; }

        /// <summary>
        /// Power reading in kilowatts.
        /// Must be non-negative.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Kw must be non-negative")]
        public decimal Kw { get; init; }

        /// <summary>
        /// State of charge percentage (0-100).
        /// </summary>
        [Range(0, 100, ErrorMessage = "StateOfChargePercent must be between 0 and 100")]
        public decimal StateOfChargePercent { get; init; }

        /// <summary>
        /// Operating status of the device.
        /// </summary>
        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Status must be between 1 and 50 characters")]
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Reported device temperature in Celsius.
        /// </summary>
        public decimal? TemperatureC { get; init; }

        /// <summary>
        /// Identifier of the gateway or device firmware source.
        /// Useful for debugging and routing.
        /// </summary>
        [Required(ErrorMessage = "GatewayId is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "GatewayId must be between 1 and 100 characters")]
        public string GatewayId { get; init; } = string.Empty;

        /// <summary>
        /// Firmware version of the device sending the reading.
        /// Used to identify firmware version.
        /// </summary>
        [Required(ErrorMessage = "FirmwareVersion is required")]
        public string FirmwareVersion { get; init; } = string.Empty;
    }
}
