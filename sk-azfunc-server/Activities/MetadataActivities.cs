using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace SK.DevTeam
{
    public static class MetadataActivities
    {
        [Function(nameof(GetMetadata))]
        public static IActionResult GetMetadata(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "metadata/{key}")] HttpRequest req,
            [TableInput("Metadata", partitionKey: "{key}", rowKey: "{key}", Connection = "AzureWebJobsStorage")] IssueMetadata metadata,
            FunctionContext executionContext)
        {
            return new OkObjectResult(metadata);
        }

        [Function(nameof(SaveMetadata))]
        [TableOutput("Metadata", Connection = "AzureWebJobsStorage")]
        public static IssueMetadata SaveMetadata(
            [ActivityTrigger] IssueMetadata metadata,
            FunctionContext executionContext)
        {
            return metadata;
        }
    }
}
