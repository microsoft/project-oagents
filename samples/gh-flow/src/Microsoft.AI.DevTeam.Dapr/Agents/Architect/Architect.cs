using CloudNative.CloudEvents;
using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AI.Agents.Dapr;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace Microsoft.AI.DevTeam;


// The architect has Org+Repo scope and is holding the knowledge of the high level architecture of the project
public class Architect : AiAgent<ArchitectState>
{
    public Architect(ActorHost host,DaprClient client, ISemanticTextMemory memory, Kernel kernel) 
    : base(host, client, memory, kernel)
    {
    }

    public override Task HandleEvent(CloudEvent item)
    {
       return Task.CompletedTask;
    }
}

public class ArchitectState
{
    public string FilesTree { get; set; }
    public string HighLevelArchitecture { get; set; }
}