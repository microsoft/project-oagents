using StackExchange.Redis;
using System.Text.Json;

namespace SupportCenter.ApiService.Data
{
    public class CustomerRepository([FromKeyedServices("redis")] IConnectionMultiplexer redisConnection, ILogger<CustomerRepository> logger) : ICustomerRepository
    {
        // Method that gets a customer by ID from redis database
        public async Task<Customer?> GetCustomerByIdAsync(string customerId)
        {
            var db = redisConnection.GetDatabase();
            var key = $"customers:{customerId}";
            var customer = await db.StringGetAsync(key);
            if (customer.IsNullOrEmpty)
            {
                return null;
            }
            return JsonSerializer.Deserialize<Customer>(customer.ToString());
        }

        public async Task<IEnumerable<Customer>> GetCustomersAsync()
        {
            var server = redisConnection.GetServer(redisConnection.GetEndPoints().First());
            var db = redisConnection.GetDatabase();
            var keys = server.Keys(pattern: "customers:*");
            var customers = new List<Customer>();

            foreach (var key in keys)
            {
                var data = await db.StringGetAsync(key);
                if (!data.IsNullOrEmpty)
                {
                    var customer = JsonSerializer.Deserialize<Customer>(data.ToString());
                    if (customer != null)
                    {
                        customers.Add(customer);
                    }
                }
            }
            return customers;
        }

        public async Task InsertCustomerAsync(Customer customer)
        {
            var db = redisConnection.GetDatabase();
            var key = $"customers:{customer.Id}";
            await db.StringSetAsync(key, JsonSerializer.Serialize(customer));
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            var db = redisConnection.GetDatabase();
            var key = $"customers:{customer.Id}";
            await db.StringSetAsync(key, JsonSerializer.Serialize(customer));
        }
    }
}
