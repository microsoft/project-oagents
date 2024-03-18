using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;

namespace Microsoft.AI.DevTeam;

public abstract class AiAgent : Agent
{
     public AiAgent(
         [PersistentState("state", "messages")] IPersistentState<AgentState> state)
    {
        _state = state;
    }
    protected readonly IPersistentState<AgentState> _state;
    protected async Task<KernelArguments> CreateWafContext(/*ISemanticTextMemory memory,*/ string ask)
    {
       // var interestingMemories = memory.SearchAsync("waf-pages", ask, 2);
        var wafContext = "Consider the following architectural guidelines:";
        // await foreach (var m in interestingMemories)
        // {
        //     wafContext += $"\n {m.Metadata.Text}";
        // }
        return new KernelArguments{
            ["input"] = ask,
            ["wafContext"] = wafContext
         };
    }

    protected void AddToHistory(string message, ChatUserType userType)
    {
        if (_state.State.History == null) _state.State.History = new List<ChatHistoryItem>();
        _state.State.History.Add(new ChatHistoryItem
        {
            Message = message,
            Order = _state.State.History.Count + 1,
            UserType = userType
        });
    }

    protected string GetChatHistory()
    {
        return string.Join("\n",_state.State.History.Select(message=> $"{message.UserType}: {message.Message}"));
    }

    protected async Task<string> CallFunction(string template, string ask, Kernel kernel/*, ISemanticTextMemory memory*/)
    {
            var function = kernel.CreateFunctionFromPrompt(template, new OpenAIPromptExecutionSettings { MaxTokens = 15000, Temperature = 0.8, TopP = 1 });
            AddToHistory(ask, ChatUserType.User);
            var history = GetChatHistory();
            var context =await CreateWafContext(ask); //await CreateWafContext(memory, history);
            var result = (await kernel.InvokeAsync(function, context)).ToString();
            
            AddToHistory(result, ChatUserType.Agent);
            await _state.WriteStateAsync();
            return result;
    }
}


[Serializable]
public class ChatHistoryItem
{
    public string Message { get; set; }
    public ChatUserType UserType { get; set; }
    public int Order { get; set; }

}

public class AgentState
{
    public List<ChatHistoryItem> History { get; set; }
    public string Understanding { get; set; }
}

public enum ChatUserType
{
    System,
    User,
    Agent
}
