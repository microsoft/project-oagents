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
using SupportCenter.SemanticKernel.Plugins.CustomerPlugin;
using SupportCenter.SignalRHub;
using static Microsoft.AI.Agents.Orleans.Resolvers;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CustomerInfo : AiAgent<CustomerInfoState>
{
    protected override string Namespace => Consts.OrleansNamespace;
    protected override string Name => nameof(CustomerInfo);
    private readonly ILogger<CustomerInfo> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICustomerRepository _customerRepository;
    private readonly IChatCompletionService _chatCompletionService;

    public CustomerInfo(
        [PersistentState("state", "messages")] IPersistentState<AgentState<CustomerInfoState>> state,
        ILogger<CustomerInfo> logger,
        IServiceProvider serviceProvider,
        ICustomerRepository customerRepository,
        KernelResolver kernelResolver,
        SemanticTextMemoryResolver memoryResolver)
    : base(state, kernelResolver, memoryResolver)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _customerRepository = customerRepository;

        _kernel.ImportPluginFromObject(serviceProvider.GetRequiredService<CustomerData>(), "CustomerPlugin");
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    public async override Task HandleEvent(Event item)
    {
        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? userMessage = item.Data.GetValueOrDefault<string>("userMessage");
        string? conversationId = SignalRConnectionsDB.GetConversationId(userId);
        string id = $"{userId}/{conversationId}";

        _logger.LogInformation("[{CustomerInfo}] Event {EventType}. Data: {EventData}", nameof(CustomerInfo), item.Type, item.Data);

        switch (item.Type)
        {
            case nameof(EventType.UserNewConversation):
                // The user started a new conversation.
                ClearHistory();
                break;
            case nameof(EventType.CustomerInfoRequested):
                await SendEvent(id, nameof(EventType.CustomerInfoNotification),
                    (nameof(userId), userId),
                    ("message", $"I'm working on the user's request..."));

                // Get the customer info via the planners.
                var prompt = CustomerInfoPrompts.GetCustomerInfo
                    .Replace("{{$userId}}", userId)
                    .Replace("{{$userMessage}}", userMessage)
                    .Replace("{{$history}}", AppendChatHistory(userMessage));

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                // HandlebarsPlanner
                // var planner = new HandlebarsPlanner();
                // var plan = await planner.CreatePlanAsync(_kernel, prompt);
                // var planResult = await plan.InvokeAsync(_kernel);
                // FunctionCallingStepwisePlanner
                var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions()
                {
                    MaxIterations = 10,
                });
                var result = await planner.ExecuteAsync(_kernel, prompt);
                await SendEvent(id, nameof(EventType.CustomerInfoRetrieved),
                    (nameof(userId), userId),
                    ("message", result.FinalAnswer));

                AddToHistory(result.FinalAnswer, ChatUserType.Agent);
#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                break;
            default:
                break;
        }
    }
}