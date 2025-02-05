namespace Microsoft.AI.Agents.Abstractions;

public interface IAiAgent : IAgent
{
    void AddToHistory(string message, ChatUserType userType);
    string AppendChatHistory(string ask);
    ValueTask<string> AddKnowledge(string instruction, string index);
}