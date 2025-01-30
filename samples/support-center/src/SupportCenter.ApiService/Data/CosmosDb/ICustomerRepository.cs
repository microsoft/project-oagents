using SupportCenter.ApiService.Data.CosmosDb.Entities;

namespace SupportCenter.ApiService.Data.CosmosDb
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetCustomerByIdAsync(string customerId);
        Task<IEnumerable<Customer>> GetCustomersAsync();
        Task InsertCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
    }
}