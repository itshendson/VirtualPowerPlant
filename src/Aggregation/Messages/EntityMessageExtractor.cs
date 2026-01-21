using Akka.Cluster.Sharding;

namespace Aggregation.Messages;

public sealed class EntityMessageExtractor : HashCodeMessageExtractor
{
    public EntityMessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
    {
    }

    public override string EntityId(object message)
    {
        if (message is not IEntityMessage entityMessage)
        {
            throw new ArgumentException(
                $"Message type {message.GetType().Name} does not implement {nameof(IEntityMessage)}.",
                nameof(message));
        }

        return entityMessage.EntityId;
    }

    public override object EntityMessage(object message)
    {
        return message;
    }
}
