using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SupportCenter.Data.CosmosDb.Entities;
using SupportCenter.Options;

namespace SupportCenter.Data.CosmosDb
{
    public class CustomerRepository : CosmosDbRepository<Customer, CosmosDbOptions>, ICustomerRepository
    {
        public CustomerRepository(IOptions<CosmosDbOptions> options, ILogger<CustomerRepository> logger)
            : base(options.Value, logger) { }

        public async Task<Customer?> GetCustomerByIdAsync(string customerId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @customerId")
                .WithParameter("@customerId", customerId);
            var iterator = Container.GetItemQueryIterator<Customer?>(query);
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
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = Container.GetItemQueryIterator<Customer>(query);
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
    }
}
