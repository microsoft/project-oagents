using CloudNative.CloudEvents;
using Dapr.Actors.Runtime;
using Microsoft.AI.Agents.Abstractions;

namespace Microsoft.AI.Agents.Dapr;

public abstract class Agent :Actor, IAgent 
{
    protected Agent(ActorHost host) : base(host)
    {
    }

    protected virtual string Namespace { get;set;}
    public abstract Task HandleEvent(CloudEvent item);


    public async Task PublishEvent(string ns, string id, CloudEvent item)
    {
        //using var client = new DaprClientBuilder().Build();
        
        // var streamProvider = this.GetStreamProvider("StreamProvider");
        // var streamId = StreamId.Create(ns, id);
        // var stream = streamProvider.GetStream<Event>(streamId);
        // await stream.OnNextAsync(item);
        
    }
}