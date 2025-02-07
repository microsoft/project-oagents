using Microsoft.AI.Agents.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Orleans.Runtime;

namespace Microsoft.AI.Agents.Orleans;

public abstract class AiAgent<T> : Agent, IAiAgent where T : class, new()
{
    protected IPersistentState<AgentState<T>> _state;
    private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;
    private readonly IVectorStore vectorStore;

    public AiAgent([PersistentState("state", "messages")] IPersistentState<AgentState<T>> state)
    {
        _state = state;
        this.embeddingGenerator = embeddingGenerator;
        this.vectorStore = vectorStore;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Initialize the Agent state
        if (_state.State.History == null) _state.State.History = new List<ChatHistoryItem>();
        if (_state.State.Data == null) _state.State.Data = new T();

        return base.OnActivateAsync(cancellationToken);
    }

    public void AddToHistory(string message, ChatUserType userType) => _state.State.History.Add(new ChatHistoryItem
    {
        Message = message,
        Order = _state.State.History.Count + 1,
        UserType = userType
    });

    public string AppendChatHistory(string ask)
    {
        AddToHistory(ask, ChatUserType.User);
        return string.Join("\n", _state.State.History.Select(message => $"{message.UserType}: {message.Message}"));
    }

    ////TODO: Implement using VectorData
    //public async ValueTask<string> AddKnowledge(string instruction, string index)
    //{
    //    var query = "A family friendly movie";
    //    var queryEmbedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(query);
    //    var collection = vectorStore.GetCollection<int, object>(index);

    //    var searchOptions = new VectorSearchOptions()
    //    {
    //        Top = 1,
    //        VectorPropertyName = "Vector"
    //    };

    //    var results = await collection.VectorizedSearchAsync(queryEmbedding, searchOptions);
    //    return "";
    //}
}