namespace Ingestion.Infrastructure.Messaging
{
    public record BufferItem<TValue>(string Topic, string Key, TValue Value, string EventId, int Attempt = 0);
}
