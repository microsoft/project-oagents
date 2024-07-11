using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Planning;
using Orleans.Runtime;
using SupportCenter.Data.CosmosDb;
using SupportCenter.Events;
using SupportCenter.Extensions;
using SupportCenter.Options;
using SupportCenter.SignalRHub;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CustomerInfo : AiAgent<CustomerInfoState>
{
    private readonly ILogger<CustomerInfo> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICustomerRepository _customerRepository;
    private readonly IChatCompletionService _chatCompletionService;
    protected override string Namespace => Consts.OrleansNamespace;

    public CustomerInfo(
        [PersistentState("state", "messages")] IPersistentState<AgentState<CustomerInfoState>> state,
        ILogger<CustomerInfo> logger,
        IServiceProvider serviceProvider,
        ICustomerRepository customerRepository,
        [FromKeyedServices("CustomerInfoKernel")] Kernel kernel)
    : base(state, default, kernel)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    public async override Task HandleEvent(Event item)
    {
        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? userMessage = item.Data.GetValueOrDefault<string>("userMessage");
        string? conversationId = SignalRConnectionsDB.GetConversationId(userId);
        string id = $"{userId}/{conversationId}";        

        switch (item.Type)
        {
            case nameof(EventType.UserNewConversation):
                // The user started a new conversation.
                _state.State.History.Clear();
                break;
            case nameof(EventType.CustomerInfoRequested):
                _logger.LogInformation("[{Agent}]:{EventType}:{EventData}", nameof(CustomerInfo), item.Type, item.Data);
                await PublishEvent(Namespace, id, new Event
                {
                    Type = nameof(EventType.CustomerInfoNotification),
                    Data = new Dictionary<string, string>
                    {
                        { nameof(userId), userId },
                        { "message", "I'm working on the user's request..." }
                    }
                });

                // Get the customer info via the planners.
                var prompt = CustomerInfoPrompts.GetCustomerInfo
                    .Replace("{{$userId}}", userId)
                    .Replace("{{$userMessage}}", userMessage)
                    .Replace("{{$history}}", AppendChatHistory(userMessage));

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                // FunctionCallingStepwisePlanner
                var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions()
                {
                    MaxIterations = 10,
                });
                var result = await planner.ExecuteAsync(_kernel, prompt);
                await PublishEvent(Namespace, id, new Event
                {
                    Type = nameof(EventType.CustomerInfoRetrieved),
                    Data = new Dictionary<string, string>
                    {
                        { nameof(userId), userId },
                        { "message", result.FinalAnswer }
                    }
                });

                AddToHistory(result.FinalAnswer, ChatUserType.Agent);
#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                break;
            default:
                break;
        }
    }
}