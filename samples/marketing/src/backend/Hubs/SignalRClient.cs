using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Marketing.Hubs
{

    public interface ISignalRClient
    {
        Task SendMessageToAll(string user, string message);
    }

    public class SignalRClient : ISignalRClient
    {
        private static ConcurrentDictionary<string, string> _connectionIdByUser { get; set; } = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, string> _allConnections { get; set; } = new ConcurrentDictionary<string, string>();
        private static ConcurrentDictionary<string, ArticleHub> _allHubs { get; set; } = new ConcurrentDictionary<string, ArticleHub>();


        private readonly IHubContext<ArticleHub> _hubContext;

        public SignalRClient(IHubContext<ArticleHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendMessageToSpecificClient(string userId, string message, AgentTypes agentType)
        {
            var connectionId = _connectionIdByUser[userId];
            var frontEndMessage = new FrontEndMessage()
            {
                UserId = userId,
                Message = message,
                Agent = agentType.ToString()
            };
            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveMessage", frontEndMessage);
        }
    }
}
