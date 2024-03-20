using Microsoft.AI.DevTeam.Abstractions;
using Microsoft.KernelMemory;
using Orleans.Runtime;
using Orleans.Streams;

namespace Microsoft.AI.DevTeam;


// The architect has Org+Repo scope and is holding the knowledge of the high level architecture of the project
[ImplicitStreamSubscription(Consts.MainNamespace)]
public class Architect : AzureAiAgent<ArchitectState>
{
    public Architect([PersistentState("state", "messages")] IPersistentState<AgentState<ArchitectState>> state, IKernelMemory memory) 
    : base(state, memory)
    {
    }

    public override Task HandleEvent(Event item, StreamSequenceToken? token)
    {
        throw new NotImplementedException();
    }
}

[GenerateSerializer]
public class ArchitectState
{
    [Id(0)]
    public string FilesTree { get; set; }
    [Id(1)]
    public string HighLevelArchitecture { get; set; }
}