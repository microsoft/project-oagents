using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Data.CosmosDb;
using SupportCenter.Events;
using SupportCenter.Options;
using SupportCenter.Plugins.CustomerPlugin;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CustomerInfo : AiAgent<CustomerInfoState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<CustomerInfo> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICustomerRepository _customerRepository;
    private readonly IChatCompletionService _chatCompletionService;

    public CustomerInfo(
        [PersistentState("state", "messages")] IPersistentState<AgentState<CustomerInfoState>> state,
        Kernel kernel,
        ISemanticTextMemory memory,
        ILogger<CustomerInfo> logger,
        IServiceProvider serviceProvider,
        ICustomerRepository customerRepository)
    : base(state, memory, kernel)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _customerRepository = customerRepository;
        _kernel = kernel;

        _kernel.ImportPluginFromObject(serviceProvider.GetRequiredService<CustomerData>(), "CustomerPlugin");
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventType.CustomerInfoRequested):
                var userId = item.Data["userId"];
                var userMessage = item.Data["userMessage"];

                // Enable auto function calling.
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                var prompt = CustomerInfoPrompts.GetCustomerInfo
                    .Replace("{{$userId}}", userId)
                    .Replace("{{$userMessage}}", userMessage);

                var msgContent = await _chatCompletionService.GetChatMessageContentAsync(prompt, openAIPromptExecutionSettings, _kernel);
                await SendCustomerInfoEvent(userId, msgContent.ToString());
                break;
            default:
                break;
        }
    }

    private async Task SendCustomerInfoEvent(string userId, string customerInfo)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventType.CustomerInfoRetrieved),
            Data = new Dictionary<string, string> {
                { nameof(userId), userId },
                { nameof(customerInfo), customerInfo}
            }
        });
    }
}