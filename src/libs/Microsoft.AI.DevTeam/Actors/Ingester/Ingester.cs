using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

public class Ingester : SemanticPersona, IIngestRepo
{
    public Ingester([PersistentState("state", "messages")] IPersistentState<ChatHistory> state) : base(state)
    {
    }

    public Task IngestionFlow(string org, string repo, string branch)
    {
        throw new NotImplementedException();
    }
}