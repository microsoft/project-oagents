using Dapr.Actors.Runtime;
using Dapr.Client;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.Extensions.AI;

namespace Microsoft.AI.Agents.Dapr;

public abstract class AiAgent<T>(ActorHost host, DaprClient client, IChatClient chatClient) : Agent(host, client), IAiAgent where T: class, new()
{
    public string StateStore = "agents-statestore";

    protected AgentState<T> state;

   
    protected override async Task OnActivateAsync()
    {
        state = await StateManager.GetOrAddStateAsync(StateStore, new AgentState<T>());
    } 

    public void AddToHistory(string message, ChatUserType userType)
    {
        if (state.History == null) state.History = new List<ChatHistoryItem>();
        state.History.Add(new ChatHistoryItem
        {
            Message = message,
            Order = state.History.Count + 1,
            UserType = userType
        });
    }

    public string AppendChatHistory(string ask)
    {
        AddToHistory(ask, ChatUserType.User);
        return string.Join("\n", state.History.Select(message => $"{message.UserType}: {message.Message}"));
    }

    public async ValueTask<string> CallFunction(string prompt, ChatOptions? chatOptions = null)
    {
        if (chatOptions == null)
        {
            chatOptions = new ChatOptions()
            {
                Temperature = 0.8f,
                TopP = 1,
                MaxOutputTokens = 4096
            };
        }
        var response = await chatClient.CompleteAsync(prompt, chatOptions);
        return response.Message.Text!;
    }

    //TODO: Implement using VectorData
    public async ValueTask<string> AddKnowledge(string instruction, string index)
    {
        return "";
    }
}
