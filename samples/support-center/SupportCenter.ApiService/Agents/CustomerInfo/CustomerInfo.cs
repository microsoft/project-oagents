using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using SupportCenter.ApiService.Data.CosmosDb;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using static SupportCenter.ApiService.Consts;

namespace SupportCenter.ApiService.Agents.CustomerInfo;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CustomerInfo(
        [PersistentState("state", "messages")] IPersistentState<AgentState<CustomerInfoState>> state,
        ILogger<CustomerInfo> logger,
        IServiceProvider serviceProvider,
        ICustomerRepository customerRepository,
       [FromKeyedServices(Gpt4oMini)] IChatClient chatClient) : AiAgent<CustomerInfoState>(state)
{
    protected override string Namespace => Consts.OrleansNamespace;

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventType.UserNewConversation):
                // The user started a new conversation.
                _state.State.History.Clear();
                break;
            case nameof(EventType.CustomerInfoRequested):
                var ssc = item.GetAgentData();
                string? userId = ssc.UserId;
                string? message = ssc.UserMessage;
                string? id = ssc.Id;

                logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(CustomerInfo), item.Type, item.Data);
                await PublishEvent(Namespace, id, new Event
                {
                    Type = nameof(EventType.CustomerInfoNotification),
                    Data = new Dictionary<string, string>
                    {
                        { nameof(userId), userId },
                        { nameof(message), "I'm working on the user's request..." }
                    }
                });

                // Get the customer info via the planners.
                var prompt = CustomerInfoPrompts.GetCustomerInfo
                    .Replace("{{$userId}}", userId)
                    .Replace("{{$userMessage}}", message)
                    .Replace("{{$history}}", AppendChatHistory(message));

                // TODO: reimplement

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                // FunctionCallingStepwisePlanner
                //var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions()
                //{
                //    MaxIterations = 10,
                //});
                //var result = await planner.ExecuteAsync(_kernel, prompt);
                //_logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(CustomerInfo), item.Type, result.FinalAnswer);

                //await PublishEvent(Namespace, id, new Event
                //{
                //    Type = nameof(EventType.CustomerInfoRetrieved),
                //    Data = new Dictionary<string, string>
                //    {
                //        { nameof(userId), userId },
                //        { nameof(message), result.FinalAnswer }
                //    }
                //});

                //AddToHistory(result.FinalAnswer, ChatUserType.Agent);
#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                break;
            default:
                break;
        }
    }
}