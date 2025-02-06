using Microsoft.Azure.Cosmos;
using SupportCenter.ApiService.Data.CosmosDb.Entities;

namespace SupportCenter.ApiService.Data.CosmosDb
{
    public class CustomerRepository(CosmosClient client, ILogger<CustomerRepository> logger) : CosmosDbRepository<Customer>(client,logger), ICustomerRepository
    {
        public async Task<Customer?> GetCustomerByIdAsync(string customerId)
        {
            var container = GetContainer();
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @customerId")
                .WithParameter("@customerId", customerId);
            var iterator = container.GetItemQueryIterator<Customer?>(query);
            Customer? customer = null;
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                customer = response.FirstOrDefault();
            }
            return customer;
        }

        public async Task<IEnumerable<Customer>> GetCustomersAsync()
        {
            var container = GetContainer();
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = container.GetItemQueryIterator<Customer>(query);
            var customers = new List<Customer>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                customers.AddRange(response);
            }
            return customers;
        }

        public async Task InsertCustomerAsync(Customer customer)
        {
            await InsertItemAsync(customer);
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await UpsertItemAsync(customer);
        }
    }
}
