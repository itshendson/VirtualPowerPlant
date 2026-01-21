using Akka.Actor;
using Aggregation.Messages;

namespace Aggregation.Actors;

public sealed class TelemetryIngressActor : ReceiveActor
{
    private readonly IActorRef _siteShard;

    public TelemetryIngressActor(IActorRef siteShard)
    {
        _siteShard = siteShard;

        Receive<SiteTelemetryReceived>(message => _siteShard.Forward(message));
    }
}
