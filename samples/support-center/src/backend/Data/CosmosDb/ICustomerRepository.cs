using SupportCenter.Data.CosmosDb.Entities;

namespace SupportCenter.Data.CosmosDb
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetCustomerByIdAsync(string customerId);
        Task<IEnumerable<Customer>> GetCustomersAsync();
        Task InsertCustomerAsync(Customer customer);
    }
}