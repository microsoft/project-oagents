using Microsoft.AI.Agents.Abstractions;
using SupportCenter.SignalRHub;

namespace SupportCenter.Extensions
{
    public static class SupportCenterAgentExtensions
    {
        public static SupportCenterData GetAgentData(this Event item)
        {
            string? userId = item.Data.GetValueOrDefault<string>("userId");
            string? userMessage = item.Data.GetValueOrDefault<string>("message")
                ?? item.Data.GetValueOrDefault<string>("userMessage");

            string? conversationId = SignalRConnectionsDB.GetConversationId(userId) ?? item.Data.GetValueOrDefault<string>("id");
            string id = $"{userId}/{conversationId}";

            return new SupportCenterData(id, userId, userMessage);
        }
    }

    public record SupportCenterData(string? Id, string? UserId, string? UserMessage);
}
