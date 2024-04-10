using CloudNative.CloudEvents;
using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AI.Agents.Abstractions;

namespace Microsoft.AI.Agents.Dapr;

public abstract class Agent : Actor, IAgent
{
    private readonly DaprClient daprClient;

    protected Agent(ActorHost host, DaprClient daprClient) : base(host)
    {
        this.daprClient = daprClient;
    }
    public abstract Task HandleEvent(CloudEvent item);

    public async Task PublishEvent(string ns, string id, CloudEvent item)
    {
       await daprClient.PublishEventAsync(ns, id, item);
    }
}