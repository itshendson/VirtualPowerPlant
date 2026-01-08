using Ingestion.Model;

namespace Ingestion.Infrastructure.Messaging
{
    public record TelemetryIngestionWorkItem(string Topic, string Key, TelemetryReadingRequest Value, string EventId);
}
