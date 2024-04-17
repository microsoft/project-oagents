using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.DevTeam;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel;
using Orleans.Runtime;
using Marketing.Hubs;
using Microsoft.AI.Agents.Orleans;

namespace Marketing.Agents.LocalAgent
{
    public class LocalAgent : AiAgent<LocalAgentState>
    {
        protected override string Namespace => Consts.OrleansNamespace;

        private readonly ILogger<LocalAgent> _logger;

        private readonly ISignalRClient _signalRClient;

        public LocalAgent([PersistentState("state", "messages")] IPersistentState<AgentState<LocalAgentState>> state, Kernel kernel, ISemanticTextMemory memory, ILogger<LocalAgent> logger, ISignalRClient signalRClient)
        : base(state, memory, kernel)
        {
            _signalRClient = signalRClient;
            _logger = logger;
            if (state.State.Data == null)
            {
                state.State.Data = new LocalAgentState();
            }
        }

        public async override Task HandleEvent(Event item)
        {
        }
    }
}
[GenerateSerializer]
public class LocalAgentState
{
    [Id(0)]
    public string WrittenPost { get; set; }
}