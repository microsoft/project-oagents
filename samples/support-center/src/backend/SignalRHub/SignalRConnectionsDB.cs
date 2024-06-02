using System.Collections.Concurrent;

namespace SupportCenter.SignalRHub;
public static class SignalRConnectionsDB
{
    public static ConcurrentDictionary<string, Connection> ConnectionIdByUser { get; } = new ConcurrentDictionary<string, Connection>();
}

public class Connection(string connectionId, string conversationId)
{
    public string? ConnectionId { get; set; } = connectionId;
    public string? ConversationId { get; set; } = conversationId;
}