using CloudNative.CloudEvents;

namespace Microsoft.AI.Agents.Abstractions;

public interface IAgent
{
    Task HandleEvent(CloudEvent item);
    Task PublishEvent(string ns, string id, CloudEvent item);
}