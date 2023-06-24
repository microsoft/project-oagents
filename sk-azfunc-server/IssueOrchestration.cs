using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace SK.DevTeam
{
    public static class IssueOrchestration
    {
         [Function("IssueOrchestrationStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "doit")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("IssueOrchestration_HttpStart");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(IssueOrchestration));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [Function(nameof(IssueOrchestration))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(IssueOrchestration));
            logger.LogInformation("Saying hello.");
            var outputs = new List<string>();

            // Get input
            await context.CallSubOrchestratorAsync(nameof(CreateReadme), "");
            var devleadResponse = await context.CallSubOrchestratorAsync<DevLeadPlanResponse>(nameof(CreatePlan), "");
            foreach(var step in devleadResponse.steps)
            {
                await context.CallSubOrchestratorAsync(nameof(Implement), "");
            }
            

            return outputs;
        }

        public static async Task CreateReadme(
        [OrchestrationTrigger] TaskOrchestrationContext context, string input)
        {
            // Create a new issue, with the input and label PM.Readme
            // Connect the new issue with the parent issue (create a new comment with this one?)
            // webhook will deal with the flow of iterating the output
            // when the new issue is closed, the output of that issue run in sandbox, commiting to a new PR
        }

        public static async Task<DevLeadPlanResponse> CreatePlan(
        [OrchestrationTrigger] TaskOrchestrationContext context, string input)
        {
            // Create a new issue, with the input and label DevLead.Plan
            // Connect the new issue with the parent issue (create a new comment with this one?)
            // webhook will deal with the flow of iterating the output
            // when the new issue is closed, the sub-orchestration finishes
            return default;
        }

        public static async Task Implement(
        [OrchestrationTrigger] TaskOrchestrationContext context, string input)
        {
            // Create a new issue, with the input and label Developer.Implement
            // Connect the new issue with the parent issue (create a new comment with this one?)
            // webhook will deal with the flow of iterating the output
            // when the new issue is closed, the output of that issue run in sandbox, commiting to a new PR
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string name, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("SayHello");
            logger.LogInformation("Saying hello to {name}.", name);
            return $"Hello {name}!";
        }
    }
}
