using Marketing.Agents;
using Marketing.Events;
using Marketing.Options;
using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.TextToImage;
using Orleans.Runtime;

namespace Marketing.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class Notary : AiAgent<NotaryState>, INotary
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<Notary> _logger;
    private readonly IConfiguration _configuration;

    public Notary([PersistentState("state", "messages")] IPersistentState<AgentState<NotaryState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<Notary> logger, IConfiguration configuration)
    : base(state, memory, kernel)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async override Task HandleEvent(Event item)
    {
        string lastMessage;

        if(_state.State.Data.AllEvents == null)
        {
            _state.State.Data.AllEvents = new List<Event>();
        }
        _state.State.Data.AllEvents.Add(item);
    }

    public Task<List<Event>> GetAllEvents()
    {
        return Task.FromResult(_state.State.Data.AllEvents);
    }
}