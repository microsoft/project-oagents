using SupportCenter.Data.CosmosDb.Entities;

namespace SupportCenter.Data.CosmosDb
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetCustomerByIdAsync(string customerId);
    }
}