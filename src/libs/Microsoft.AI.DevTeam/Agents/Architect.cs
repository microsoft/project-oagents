using Orleans.Runtime;
using Orleans.Streams;

namespace Microsoft.AI.DevTeam;

[ImplicitStreamSubscription(Consts.MainNamespace)]
public class Architect : AiAgent<ArchitectState>
{
    public Architect([PersistentState("state", "messages")] IPersistentState<ArchitectState> state) : base(state)
    {
    }

    public override Task HandleEvent(Event item, StreamSequenceToken? token)
    {
        throw new NotImplementedException();
    }
}

public class ArchitectState : AgentState
{
   public string FilesTree { get; set; }
   public string HighLevelArchitecture { get; set; }
}

// The architect has Org+Repo scope and is holding the knowledge of the high level architecture of the project