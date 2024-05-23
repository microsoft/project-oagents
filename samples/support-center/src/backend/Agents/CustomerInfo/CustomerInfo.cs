using Microsoft.AI.Agents.Abstractions;
using Microsoft.AI.Agents.Orleans;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Orleans.Runtime;
using SupportCenter.Data.CosmosDb;
using SupportCenter.Events;
using SupportCenter.Options;

namespace SupportCenter.Agents;

[ImplicitStreamSubscription(Consts.OrleansNamespace)]
public class CustomerInfo : AiAgent<CustomerInfoState>
{
    protected override string Namespace => Consts.OrleansNamespace;

    private readonly ILogger<CustomerInfo> _logger;
    private readonly ICustomerRepository _customerRepository;

    public CustomerInfo(
        [PersistentState("state", "messages")] IPersistentState<AgentState<CustomerInfoState>> state,
        Kernel kernel,
        ISemanticTextMemory memory,
        ILogger<CustomerInfo> logger,
        ICustomerRepository customerRepository)
    : base(state, memory, kernel)
    {
        _logger = logger;
        _customerRepository = customerRepository;
    }

    public async override Task HandleEvent(Event item)
    {
        switch (item.Type)
        {
            case nameof(EventTypes.CustomerInfoRequested):
                // Handle customer info request
                string userId = item.Data["UserId"];
                Customer? customer = await GetCustomerInfoAsync(userId);
                if (customer == null)
                {
                    _logger.LogWarning("[{Agent}] Customer info not found for user {userId}", nameof(CustomerInfo), userId);
                    return;
                }
                await SendCustomerInfoEvent(userId, customer.Name ?? "No User");
                break;
            default:
                break;
        }
    }

    private async Task<Customer?> GetCustomerInfoAsync(string userId)
    {
        var customer = await _customerRepository.GetCustomerByIdAsync(userId);
        if (customer == null)
        {
            _logger.LogWarning("[{Agent}] Customer not found for user {userId}", nameof(CustomerInfo), userId);
            return null;
        }
        return new Customer
        {
            Name = customer?.Name,
            Email = customer?.Email,
            Phone = customer?.Phone
        };
    }

    private async Task SendCustomerInfoEvent(string userId, string customerInfo)
    {
        await PublishEvent(Consts.OrleansNamespace, this.GetPrimaryKeyString(), new Event
        {
            Type = nameof(EventTypes.CustomerInfoProvided),
            Data = new Dictionary<string, string> {
                { "UserId", userId },
                { nameof(customerInfo), customerInfo}
            }
        });
    }
}