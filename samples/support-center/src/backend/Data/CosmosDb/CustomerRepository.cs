using Microsoft.Azure.Cosmos;
using SupportCenter.Data.CosmosDb.Configurations;
using SupportCenter.Data.CosmosDb.Entities;

namespace SupportCenter.Data.CosmosDb
{
    public class CustomerRepository : CosmosDbRepository<Customer, CosmosDbConfiguration>, ICustomerRepository
    {
        public CustomerRepository(CosmosDbConfiguration options, ILogger logger) : base(options, logger) { }

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
    }
}
