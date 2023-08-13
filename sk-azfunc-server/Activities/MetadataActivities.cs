using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace SK.DevTeam
{
    public static class MetadataActivities
    {
        [Function(nameof(GetMetadata))]
        public static IActionResult GetMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metadata/{number:int}")] HttpRequest req,
            [CosmosDBInput(
                databaseName:"dev-db",
                collectionName:"devs",
                ConnectionStringSetting= "CosmosConnectionString",
                SqlQuery= "SELECT * FROM c where c.number = StringToNumber({number})")] IEnumerable<IssueMetadata> items,
            FunctionContext executionContext)
        {
            return new OkObjectResult(items.First());
        }

        [Function(nameof(SaveMetadata))]
        [CosmosDBOutput("dev-db", "devs", CreateIfNotExists = true, ConnectionStringSetting = "CosmosConnectionString", PartitionKey = "/id")]
        public static IssueMetadata SaveMetadata(
            [ActivityTrigger] IssueMetadata metadata,
            FunctionContext executionContext)
        {
            return metadata;
        }
    }
}
