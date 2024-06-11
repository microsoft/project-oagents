using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using Orleans.Runtime;
using SupportCenter.Data.CosmosDb;
using SupportCenter.Events;
using SupportCenter.Extensions;
using SupportCenter.Options;
using SupportCenter.SemanticKernel.Plugins.CustomerPlugin;
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
    protected override Kernel Kernel { get; }
    protected override ISemanticTextMemory Memory { get; }

    public CustomerInfo(
        [PersistentState("state", "messages")] IPersistentState<AgentState<CustomerInfoState>> state,
        ILogger<CustomerInfo> logger,
        IServiceProvider serviceProvider,
        ICustomerRepository customerRepository,
        [FromKeyedServices("CustomerInfoKernel")] Kernel kernel,
        [FromKeyedServices("CustomerInfoMemory")] ISemanticTextMemory memory)
    : base(state)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        Memory = memory ?? throw new ArgumentNullException(nameof(memory));

        if (Kernel.Plugins.TryGetPlugin("CustomerPlugin", out var plugin) == false)
            Kernel.ImportPluginFromObject(serviceProvider.GetRequiredService<CustomerData>(), "CustomerPlugin");
        _chatCompletionService = Kernel.GetRequiredService<IChatCompletionService>();
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
                var result = await planner.ExecuteAsync(Kernel, prompt);
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