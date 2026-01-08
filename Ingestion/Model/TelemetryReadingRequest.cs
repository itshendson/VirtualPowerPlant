using System.ComponentModel.DataAnnotations;

namespace Ingestion.Model
{
    /// <summary>
    /// Represents a single telemetry reading sent by an edge gateway.
    /// This is the HTTP boundary model for ingestion.
    /// </summary>
    public sealed class TelemetryReadingRequest
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
        public DateTimeOffset ReadingTime { get; init; }

        /// <summary>
        /// Power reading in kilowatts.
        /// Must be non-negative.
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Kw must be non-negative")]
        public decimal Kw { get; init; }

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
        [Required(ErrorMessage = "FirmwareVersion is required")]
        public string FirmwareVersion { get; init; } = string.Empty;
    }
}
