namespace Microsoft.AI.Agents.Abstractions;

[Serializable]
public class ChatHistoryItem
{
    public string Message { get; set; } = string.Empty;
    public ChatUserType UserType { get; set; }
    public int Order { get; set; }

}
