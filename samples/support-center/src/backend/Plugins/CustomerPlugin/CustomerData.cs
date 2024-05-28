using Microsoft.SemanticKernel;
using SupportCenter.Data.CosmosDb;
using System.ComponentModel;
using SupportCenter.Data.CosmosDb.Entities;

namespace SupportCenter.Plugins.CustomerPlugin
{
    public class CustomerData
    {
        //private readonly ILogger<CustomerData> _logger;
        private readonly ICustomerRepository _customerRepository;

        public CustomerData(
            ILogger<CustomerData> logger,
            ICustomerRepository customerRepository)
        {
            //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        }

        [KernelFunction, Description("Get customer data")]
        public async Task<Customer> GetCustomerDataAsync(
            [Description("The customer id")] string customerId)
        {
            //_logger.LogInformation("Executing {FunctionName} function. Params: {parameters}", nameof(GetCustomerDataAsync), string.Join(", ", [customerId]));
            var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                //_logger.LogWarning("Customer with id {customerId} not found", customerId);
            }
            return customer ?? new Customer();
        }
    }
}
