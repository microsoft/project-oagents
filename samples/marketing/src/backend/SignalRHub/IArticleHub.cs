using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam.Events;
using Microsoft.AI.DevTeam;
using Orleans.Runtime;
using System.Text.RegularExpressions;

namespace Marketing.SignalRHub;

public interface IArticleHub
{
    public Task ConnectToAgent(string UserId);

    public Task ChatMessage(FrontEndMessage frontEndMessage, IClusterClient clusterClient);

    public Task SendMessageToSpecificClient(string userId, string message);
}
