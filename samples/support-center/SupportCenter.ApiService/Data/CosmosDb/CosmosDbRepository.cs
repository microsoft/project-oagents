using Microsoft.Azure.Cosmos;
using SupportCenter.ApiService.Data.Entities;

namespace SupportCenter.ApiService.Data.CosmosDb
{
    public abstract class CosmosDbRepository<T>(CosmosClient client, ILogger<CosmosDbRepository<T>> logger)
        where T : Entity
    {
        protected Container GetContainer()
        {
            var database = client.GetDatabase("supportcenter");
            var container = database.GetContainer("items");
            return container;
        }
        public async Task<TOutput> GetItemAsync<TOutput>(string id, string partitionKey)
        {
            var container = GetContainer();
            TOutput item = await container.ReadItemAsync<TOutput>(id: id, partitionKey: new PartitionKey(partitionKey));
            return item;
        }

        public async Task InsertItemAsync(T entity)
        {
            try
            {
                var container = GetContainer();
                var response = await container.CreateItemAsync(entity, new PartitionKey(entity.GetPartitionKeyValue()));
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    "An error occurred. MethodName: {methodName} ErrorMessage: {errorMessage}",
                    nameof(InsertItemAsync),
                    ex.Message
                );

                throw;
            }
        }

        public async Task UpsertItemAsync(T entity)
        {
            var container = GetContainer();
            await container.UpsertItemAsync(entity, new PartitionKey(entity.GetPartitionKeyValue()));
        }
    }
}
