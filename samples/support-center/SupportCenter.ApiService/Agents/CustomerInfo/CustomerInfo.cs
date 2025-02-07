using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.Extensions.AI;
using SupportCenter.ApiService.Data;
using SupportCenter.ApiService.Events;
using SupportCenter.ApiService.Extensions;
using System.ComponentModel;
using static SupportCenter.ApiService.Consts;

namespace SupportCenter.ApiService.Agents.CustomerInfo;

[ImplicitStreamSubscription(OrleansNamespace)]
public class CustomerInfo(
        [PersistentState("state", "messages")] IPersistentState<AgentState<CustomerInfoState>> state,
        ILogger<CustomerInfo> logger,
        IServiceProvider serviceProvider,
        ICustomerRepository customerRepository,
       [FromKeyedServices(Gpt4oMini)] IChatClient chatClient) : AiAgent<CustomerInfoState>(state)
{
    protected override string Namespace => OrleansNamespace;

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
                var prompt = $"""
                                Here is the user message:
                                userId: {userId}
                                userMessage: {message}

                                Here is the history of all messages (including the last one):
                                {AppendChatHistory(message)}
                                """;

                
                var chatOptions = new ChatOptions
                {
                    Tools = [AIFunctionFactory.Create(GetCustomerDataAsync),
                        AIFunctionFactory.Create(GetAllCustomersAsync),
                        AIFunctionFactory.Create(InsertCustomerDataAsync),
                        AIFunctionFactory.Create(UpdateCustomerDataAsync)
                    ]
                };

                List<ChatMessage> chatHistory = [new(ChatRole.System, """
                    You are a Customer Info agent, working with the Support Center.
                    You can help customers working with their own information.
                    Read the customer's message carefully, and then decide the appropriate plan to create.
                    A history of the conversation is available to help you building a correct plan.
                    If you don't know how to proceed, don't guess; instead ask for more information and use it as a final answer.
                    If you think that the message is not clear, you can ask the customer for more information.
                """)];

                chatHistory.Add(new ChatMessage(ChatRole.User, prompt));

                var response = await chatClient.CompleteAsync(chatHistory, chatOptions);
                var result = response.Message.Contents.Last();
                AddToHistory($"{result}", ChatUserType.Agent);
               
                logger.LogInformation("[{Agent}]:[{EventType}]:[{EventData}]", nameof(CustomerInfo), item.Type, result);

                await PublishEvent(Namespace, id, new Event
                {
                    Type = nameof(EventType.CustomerInfoRetrieved),
                    Data = new Dictionary<string, string>
                    {
                        { nameof(userId), userId },
                        { nameof(message), $"{result}" }
                    }
                });

                break;
            default:
                break;
        }
    }

    [Description("Get customer data")]
    public async Task<Customer> GetCustomerDataAsync(
        [Description("The customer id")] string customerId)
    {
        logger.LogInformation("Executing {FunctionName} function. Params: {parameters}", nameof(GetCustomerDataAsync), string.Join(", ", [customerId]));
        var customer = await customerRepository.GetCustomerByIdAsync(customerId);
        if (customer == null)
        {
            logger.LogWarning("Customer with id {customerId} not found", customerId);
        }
        return customer ?? new Customer();
    }

    [Description("Get all customers")]
    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        logger.LogInformation("Executing {FunctionName} function", nameof(GetAllCustomersAsync));
        return await customerRepository.GetCustomersAsync();
    }

    [Description("Insert customer data")]
    public async Task InsertCustomerDataAsync(
        [Description("The customer data")] Customer customer)
    {
        logger.LogInformation("Executing {FunctionName} function. Params: {parameters}", nameof(InsertCustomerDataAsync), customer.ToStringCustom());
        await customerRepository.InsertCustomerAsync(customer);
    }

    [Description("Update customer data")]
    public async Task UpdateCustomerDataAsync(
        [Description("The customer data")] Customer customer)
    {
        logger.LogInformation("Executing {FunctionName} function. Params: {parameters}", nameof(UpdateCustomerDataAsync), customer.ToStringCustom());
        await customerRepository.UpdateCustomerAsync(customer);
    }
}