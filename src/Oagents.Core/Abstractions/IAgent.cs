using CloudNative.CloudEvents;

namespace Microsoft.AI.Agents.Abstractions;

public interface IAgent
{
    Task HandleEvent(Event item);
    Task PublishEvent(string ns, string id, Event item);
    
    // CloudEvent overloads for direct CloudEvent support
    Task HandleEvent(CloudEvent item);
    Task PublishEvent(string ns, string id, CloudEvent item);
}