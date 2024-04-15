using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam.Events;
using Microsoft.AI.DevTeam;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using Polly.CircuitBreaker;
using System.Collections.Concurrent;

namespace Marketing.Hubs
{
    public class ArticleHub: Hub<IArticleHub>
    {
        private readonly IHubContext<ArticleHub> _hubContext;
        public static ConcurrentDictionary<string, string> _connectionIdByUser { get; set; } = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> _allConnections { get; set; } = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, ArticleHub> _allHubs { get; set; } = new ConcurrentDictionary<string, ArticleHub>();

        public ArticleHub(IHubContext<ArticleHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public override async Task OnConnectedAsync()
        {
            _allConnections.TryAdd(Context.ConnectionId, string.Empty);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string removedUserId;
            _allConnections.TryRemove(Context.ConnectionId, out removedUserId);
            _connectionIdByUser.TryRemove(removedUserId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task ConnectToAgent(string UserId)
        {
            var frontEndMessage = new FrontEndMessage()
            {
                UserId = UserId,
                Message = "Connected to agents",
                Agent = AgentTypes.Chat.ToString()
            };

            _allHubs.TryAdd(UserId, this);
            _connectionIdByUser.AddOrUpdate(UserId, Context.ConnectionId, (key, oldValue) => Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName: UserId);
            //await Clients.Group(UserId).SendAsync(method: "ReceiveMessage", arg1: frontEndMessage);
        }

        public async Task ChatMessage(FrontEndMessage frontEndMessage, IClusterClient clusterClient)
        {
            var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create(Consts.OrleansNamespace, frontEndMessage.UserId);
            var stream = streamProvider.GetStream<Event>(streamId);

            var data = new Dictionary<string, string>
            {
                { "UserId", frontEndMessage.UserId },
                { "userMessage", frontEndMessage.Message},
            };

            await stream.OnNextAsync(new Event
            {
                Type = nameof(EventTypes.UserChatInput),
                Message = frontEndMessage.Message,
                Data = data
            });
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
