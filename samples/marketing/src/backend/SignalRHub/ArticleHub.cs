namespace Marketing.SignalRHub;

using Microsoft.AI.Agents.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Orleans.Runtime;
using Marketing.Options;
using Marketing.Events;

public class ArticleHub : Hub<IArticleHub>
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        string removedSessionId;
        SignalRConnectionsDB.ConnectionIdByUser.TryRemove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// This method is called when a new message from the client arrives.
    /// </summary>
    /// <param name="frontEndMessage"></param>
    /// <param name="clusterClient"></param>
    /// <returns></returns>
    public async Task ProcessMessage(FrontEndMessage frontEndMessage, IClusterClient clusterClient)
    {
        var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.OrleansNamespace, frontEndMessage.SessionId);
        var stream = streamProvider.GetStream<Event>(streamId);

        var data = new Dictionary<string, string>
            {
                { "SessionId", frontEndMessage.SessionId },
                { "userMessage", frontEndMessage.Message},
            };

        await stream.OnNextAsync(new Event
        {
            Type = nameof(EventTypes.UserChatInput),
            Data = data
        });

    }

    public async Task ConnectToAgent(string SessionId, IClusterClient clusterClient)
    {
        var frontEndMessage = new FrontEndMessage()
        {
            SessionId = SessionId,
            Message = "Connected to agents",
            Agent = AgentTypes.Writer.ToString()
        };

        SignalRConnectionsDB.ConnectionIdByUser.AddOrUpdate(SessionId, Context.ConnectionId, (key, oldValue) => Context.ConnectionId);

        // Notify the agents that a new user got connected.
        var streamProvider = clusterClient.GetStreamProvider("StreamProvider");
        var streamId = StreamId.Create(Consts.OrleansNamespace, frontEndMessage.SessionId);
        var stream = streamProvider.GetStream<Event>(streamId);
        var data = new Dictionary<string, string>
            {
                { "SessionId", frontEndMessage.SessionId },
                { "userMessage", frontEndMessage.Message},
            };
        await stream.OnNextAsync(new Event
        {
            Type = nameof(EventTypes.UserConnected),
            Data = data
        });
    }
}