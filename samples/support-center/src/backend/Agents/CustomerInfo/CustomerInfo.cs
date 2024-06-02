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
        string? messageId = item.Data.GetValueOrDefault<string>("id");
        string? userId = item.Data.GetValueOrDefault<string>("userId");
        string? userMessage = item.Data.GetValueOrDefault<string>("userMessage");

        switch (item.Type)
        {
            case nameof(EventType.UserConnected):
                // The user reconnected, let's send the last message if we have one
                string? lastMessage = _state.State.History.LastOrDefault()?.Message;
                if (lastMessage == null)
                {
                    return;
                }
                //await SendDispatcherEvent(userId, lastMessage, item.Data["userId"]);
                break;
            case nameof(EventType.CustomerInfoRequested):
                var prompt = CustomerInfoPrompts.GetCustomerInfo
                    .Replace("{{$userId}}", userId)
                    .Replace("{{$userMessage}}", userMessage);
#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var planner = new FunctionCallingStepwisePlanner();
                var result = await planner.ExecuteAsync(_kernel, prompt);
                await SendCustomerInfoEvent(messageId, userId, result.FinalAnswer);
#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                break;
            default:
                break;
        }
    }

    private async Task SendCustomerInfoEvent(string id, string userId, string answer)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventType.CustomerInfoRetrieved),
            Data = new Dictionary<string, string> {
                { nameof(id), id },
                { nameof(userId), userId },
                { nameof(answer), answer}
            }
        });
    }
}