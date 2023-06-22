using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.IssueComment;
using Octokit.Webhooks.Events.Issues;
using skills;

public class SKWebHookEventProcessor : WebhookEventProcessor
{
    private readonly IKernel _kernel;

    public SKWebHookEventProcessor(IKernel kernel)
    {
        _kernel = kernel;
    }
    protected async override Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        var ghClient = await GetGitHubClient();
        var org=issuesEvent.Organization.Login;
        var repo = issuesEvent.Repository.Name;
        var issueNumber = issuesEvent.Issue.Number;

        // Assumes the label follows the following convention: Skill.Function example: PM.Readme
        var labels = issuesEvent.Issue.Labels.First().Name.Split(".");
        var skillName =labels[0];
        var functionName = labels[1];
        var input = issuesEvent.Issue.Body;
        var result = await RunSkill(skillName, functionName, input);

        await ghClient.Issue.Comment.Create(org, repo, (int)issueNumber, result);

        // try
        // {
        //     // Get the payload
        //     // check what type it is
        //     // handle each type
        //     // // issue opened
        //         // read the issue object, the body has the input, the label has the skill
        //     // // issue-comment created - edited
        //        // this is a follow up of the previous action, the body and the label are important
        //        // if the label is a Skill, take the body and run the skill, post the result as new comment 
        //        // if the body is Yes/No, re-run the previous skill?
        //     // run the skill and return the output as a new issue comment
        //     // deal with the side effects
    }

    protected async override Task ProcessIssueCommentWebhookAsync(
        WebhookHeaders headers,
        IssueCommentEvent issueCommentEvent,
        IssueCommentAction action)
    {
        // we only resond to non-bot comments
        if (issueCommentEvent.Sender.Type.StringValue != "Bot")
        {
            var ghClient = await GetGitHubClient();
            var org=issueCommentEvent.Organization.Login;
            var repo = issueCommentEvent.Repository.Name;      
            var issueId = issueCommentEvent.Issue.Number;
            
            
            // Assumes the label follows the following convention: Skill.Function example: PM.Readme
            var labels = issueCommentEvent.Issue.Labels.First().Name.Split(".");
            var skillName =labels[0];
            var functionName = labels[1];
            var input = issueCommentEvent.Comment.Body;
            var result = await RunSkill(skillName, functionName, input);

            await ghClient.Issue.Comment.Create(org, repo, (int)issueId, result);
        }
        
    }

    private async Task<string> RunSkill(string skillName, string functionName, string input)
    {
        var skillConfig = SemanticFunctionConfig.ForSkillAndFunction(skillName, functionName);
        var function = _kernel.CreateSemanticFunction(skillConfig.PromptTemplate, skillConfig.Name, skillConfig.SkillName,
                                                   skillConfig.Description, skillConfig.MaxTokens, skillConfig.Temperature,
                                                   skillConfig.TopP, skillConfig.PPenalty, skillConfig.FPenalty);

        var interestingMemories = _kernel.Memory.SearchAsync("waf-pages", input, 2);
        var wafContext = "Consider the following architectural guidelines:";
        await foreach (var memory in interestingMemories)
        {
            wafContext += $"\n {memory.Metadata.Text}";
        }

        var context = new ContextVariables();
        context.Set("input", input);
        context.Set("wafContext", wafContext);

        var result = await _kernel.RunAsync(context, function).ConfigureAwait(false);
        return result.ToString();
    }

    

    private static async Task<GitHubClient> GetGitHubClient()
    {
        var key = Environment.GetEnvironmentVariable("GH_APP_KEY", EnvironmentVariableTarget.Process);
        var appId = int.Parse(Environment.GetEnvironmentVariable("GH_APP_ID", EnvironmentVariableTarget.Process));
        var installationId = int.Parse(Environment.GetEnvironmentVariable("GH_INST_ID", EnvironmentVariableTarget.Process));
        
        // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
        var generator = new GitHubJwt.GitHubJwtFactory(
            new GitHubJwt.StringPrivateKeySource(key),
            new GitHubJwt.GitHubJwtFactoryOptions
            {
                AppIntegrationId = appId, // The GitHub App Id
                ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
            }
        );

        var jwtToken = generator.CreateEncodedJwtToken();
        var appClient = new GitHubClient(new ProductHeaderValue("SK-DEV-APP"))
        {
            Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
        };
        var response = await appClient.GitHubApps.CreateInstallationToken(installationId);
        return new GitHubClient(new ProductHeaderValue($"SK-DEV-APP-Installation{installationId}"))
        {
            Credentials = new Credentials(response.Token)
        };
    }
}