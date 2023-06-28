using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Octokit;

namespace SK.DevTeam
{
    public static class IssueOrchestration
    {
         [Function("IssueOrchestrationStart")]
        public static async Task<string> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "doit")] HttpRequest req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("IssueOrchestration_HttpStart");
            var request =  await req.ReadFromJsonAsync<IssueOrchestrationRequest>();
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(IssueOrchestration), request);

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
            return "";
        }

        [Function(nameof(IssueOrchestration))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context, IssueOrchestrationRequest request)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(IssueOrchestration));
            var outputs = new List<string>();

            // Get input
            await context.CallSubOrchestratorAsync(nameof(CreateReadme), request);
            var devleadResponse = await context.CallSubOrchestratorAsync<DevLeadPlanResponse>(nameof(CreatePlan), request);
            foreach(var step in devleadResponse.steps)
            {
                await context.CallSubOrchestratorAsync(nameof(Implement), request);
            }
            return outputs;
        }

        [Function(nameof(CreateReadme))]
        public static async Task CreateReadme(
        [OrchestrationTrigger] TaskOrchestrationContext context, IssueOrchestrationRequest request)
        {
            // call activity to create new issue
            var newIssue = await context.CallActivityAsync<NewIssue>(nameof(CreateIssue), new NewIssueRequest{
                IssueRequest = request,
                Skill = nameof(skills.PM),
                Function = nameof(skills.PM.Readme)
            });
            
            // Create a new issue, with the input and label PM.Readme
            // Connect the new issue with the parent issue (create a new comment with this one?)
            // webhook will deal with the flow of iterating the output
            // when the new issue is closed, the output of that issue run in sandbox, commiting to a new PR
        }

         [Function(nameof(CreatePlan))]
        public static async Task<DevLeadPlanResponse> CreatePlan(
        [OrchestrationTrigger] TaskOrchestrationContext context, IssueOrchestrationRequest request)
        {
            // Create a new issue, with the input and label DevLead.Plan
            var newIssue = await context.CallActivityAsync<NewIssue>(nameof(CreateIssue), new NewIssueRequest{
                IssueRequest = request,
                Skill = nameof(skills.DevLead),
                Function = nameof(skills.DevLead.Plan)
            });
            // Connect the new issue with the parent issue (create a new comment with this one?)
            // webhook will deal with the flow of iterating the output
            // when the new issue is closed, the sub-orchestration finishes
            return default;
        }


         [Function(nameof(Implement))]
        public static async Task Implement(
        [OrchestrationTrigger] TaskOrchestrationContext context, IssueOrchestrationRequest request)
        {
            // Create a new issue, with the input and label Developer.Implement
            var newIssue = await context.CallActivityAsync<NewIssue>(nameof(CreateIssue), new NewIssueRequest{
                IssueRequest = request,
                Skill = nameof(skills.Developer),
                Function = nameof(skills.Developer.Implement)
            });
            
            // Connect the new issue with the parent issue (create a new comment with this one?)
            // webhook will deal with the flow of iterating the output
            // when the new issue is closed, the output of that issue run in sandbox, commiting to a new PR
        }

        [Function(nameof(CreateIssue))]
        public static async Task<NewIssue> CreateIssue([ActivityTrigger] NewIssueRequest request, FunctionContext executionContext)
        {
            var ghClient = await GithubService.GetGitHubClient();
            var newIssue = new NewIssue($"{request.Function} chain") {
                Body = request.IssueRequest.Input
            };
            newIssue.Labels.Add($"{request.Skill}.{request.Function}");
            var issue = await ghClient.Issue.Create(request.IssueRequest.Org,request.IssueRequest.Repo, newIssue);
            await ghClient.Issue.Comment.Create(request.IssueRequest.Org,request.IssueRequest.Repo, (int)request.IssueRequest.Number, $"#{issue.Number} tracks {request.Skill}.{request.Function}");
            return newIssue;
        }
    }
}

public class NewIssueRequest 
{
    public IssueOrchestrationRequest IssueRequest { get; set; }
    public string Skill { get; set; }
    public string Function { get; set; }
}
