using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AI.Agents.Abstractions;
using CloudNative.CloudEvents;

namespace Microsoft.AI.Agents.Dapr;

public abstract class Agent : Actor, IAgent
{
    private readonly DaprClient daprClient;

    protected Agent(ActorHost host, DaprClient daprClient) : base(host)
    {
        this.daprClient = daprClient;
    }
    
    public abstract Task HandleEvent(Event item);

    // CloudEvent overload - default implementation converts to Event
    public virtual Task HandleEvent(CloudEvent item)
    {
        return HandleEvent(new Event(item));
    }

    public async Task PublishEvent(string ns, string id, Event item)
    {
        var metadata = new Dictionary<string, string>() {
                 { "cloudevent.Type", item.Type },
                 { "cloudevent.Subject",  item.Subject },
                 { "cloudevent.id", Guid.NewGuid().ToString()}
            };
      
       await daprClient.PublishEventAsync(ns, id, item, metadata);
    }

    // CloudEvent overload - publishes CloudEvent directly with proper metadata
    public async Task PublishEvent(string ns, string id, CloudEvent item)
    {
        var metadata = new Dictionary<string, string>() {
                 { "cloudevent.Type", item.Type ?? string.Empty },
                 { "cloudevent.Subject",  item.Subject ?? string.Empty },
                 { "cloudevent.id", item.Id ?? Guid.NewGuid().ToString()},
                 { "cloudevent.Source", item.Source?.ToString() ?? string.Empty}
            };
      
       await daprClient.PublishEventAsync(ns, id, item, metadata);
    }
}
