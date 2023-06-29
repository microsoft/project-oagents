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


        [Function("CloseSubOrchestration")]
        public static async Task Close(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "close")] HttpRequest req,
            [DurableClient] DurableTaskClient client)
        {
            var request =  await req.ReadFromJsonAsync<IssueOrchestrationRequest>();
            await client.RaiseEventAsync(instanceId, "Approval", true);
        }

        [Function(nameof(IssueOrchestration))]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context, IssueOrchestrationRequest request)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(IssueOrchestration));
           
            var outputs = new List<string>();

            var readmeTask = context.CallSubOrchestratorAsync(nameof(CreateReadme), request);
            var planTask =  context.CallSubOrchestratorAsync<DevLeadPlanResponse>(nameof(CreatePlan), request);
            await Task.WhenAll(readmeTask, planTask);
            var implementationTasks = planTask.Result.steps.SelectMany(s => s.subtasks.Select(st => 
                        context.CallSubOrchestratorAsync(nameof(Implement), new IssueOrchestrationRequest{
                            Number = request.Number,
                            Org = request.Org,
                            Repo = request.Repo,
                            Input = st.LLM_prompt
                        })));
            await Task.WhenAll(implementationTasks);
            return outputs;
        }

        [Function(nameof(CreateReadme))]
        public static async Task CreateReadme(
        [OrchestrationTrigger] TaskOrchestrationContext context, IssueOrchestrationRequest request)
        {
            // call activity to create new issue
            var newIssue = await context.CallActivityAsync<Issue>(nameof(CreateIssue), new NewIssueRequest{
                IssueRequest = request,
                Skill = nameof(skills.PM),
                Function = nameof(skills.PM.Readme)
            });
            bool approved = await context.WaitForExternalEvent<bool>("IssueClosed");

            var lastComment = await context.CallActivityAsync<IssueComment>(nameof(GetLastComment), new IssueOrchestrationRequest {
                Org = request.Org,
                Repo = request.Repo,
                Number = newIssue.Number
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

            bool approved = await context.WaitForExternalEvent<bool>("IssueClosed");
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
            var newIssue = await context.CallActivityAsync<Issue>(nameof(CreateIssue), new NewIssueRequest{
                IssueRequest = request,
                Skill = nameof(skills.Developer),
                Function = nameof(skills.Developer.Implement)
            });
            
            bool approved = await context.WaitForExternalEvent<bool>("IssueClosed");

            var lastComment = await context.CallActivityAsync<IssueComment>(nameof(GetLastComment), new IssueOrchestrationRequest {
                Org = request.Org,
                Repo = request.Repo,
                Number = newIssue.Number
            });
            // Connect the new issue with the parent issue (create a new comment with this one?)
            // webhook will deal with the flow of iterating the output
            // when the new issue is closed, the output of that issue run in sandbox, commiting to a new PR
        }

        [Function(nameof(CreateIssue))]
        public static async Task<Issue> CreateIssue([ActivityTrigger] NewIssueRequest request, FunctionContext executionContext)
        {
            
            var ghClient = await GithubService.GetGitHubClient();
            var newIssue = new NewIssue($"{request.Function} chain") {
                Body = request.IssueRequest.Input,
                
            };
            
            // TODO: add the orchestration id as label?
            newIssue.Labels.Add($"{request.Skill}.{request.Function}");
            var issue = await ghClient.Issue.Create(request.IssueRequest.Org,request.IssueRequest.Repo, newIssue);
            await ghClient.Issue.Comment.Create(request.IssueRequest.Org,request.IssueRequest.Repo, (int)request.IssueRequest.Number, $"#{issue.Number} tracks {request.Skill}.{request.Function}");
            return issue;
        }

        [Function(nameof(GetLastComment))]
        public static async Task<IssueComment> GetLastComment([ActivityTrigger] IssueOrchestrationRequest request, FunctionContext executionContext)
        {
            var ghClient = await GithubService.GetGitHubClient();
            var icOptions = new IssueCommentRequest {
                Direction = SortDirection.Descending
            };
            var apiOptions = new ApiOptions {
                PageCount = 1,
                PageSize = 1,
                StartPage = 1
            };
            var comments = await ghClient.Issue.Comment.GetAllForIssue(request.Org, request.Repo, (int)request.Number, icOptions, apiOptions); 
            return comments.First();
        }

        [Function(nameof(RunInSandbox))]
        public static async Task<IssueComment> RunInSandbox([ActivityTrigger] IssueOrchestrationRequest request, FunctionContext executionContext)
        {
            
            //TODO
            return default;
        }
    }
}

public class NewIssueRequest 
{
    public IssueOrchestrationRequest IssueRequest { get; set; }
    public string Skill { get; set; }
    public string Function { get; set; }
}
