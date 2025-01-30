using System.Collections.Concurrent;

namespace SupportCenter.ApiService.SignalRHub;
public static class SignalRConnectionsDB
{
    public static ConcurrentDictionary<string, Connection> ConnectionByUser { get; } = new ConcurrentDictionary<string, Connection>();

    // Get conversationId by userId
    public static string? GetConversationId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (ConnectionByUser.TryGetValue(userId, out var connection))
        {
            return connection.ConversationId;
        }
        return null;
    }
}

public class Connection(string connectionId, string conversationId)
{
    public string? Id { get; set; } = connectionId;
    public string? ConversationId { get; set; } = conversationId;

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        var other = (Connection)obj;
        return Id == other.Id && ConversationId == other.ConversationId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, ConversationId);
    }

}