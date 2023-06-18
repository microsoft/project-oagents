using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Models;
using Octokit;
using skills;

public class ExecuteFunctionEndpoint
{
    private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IKernel _kernel;

    public ExecuteFunctionEndpoint(IKernel kernel)
    {
        this._kernel = kernel;
    }

    [Function("ExecuteFunction")]
    [OpenApiOperation(operationId: "ExecuteFunction", tags: new[] { "ExecuteFunction" }, Description = "Execute the specified semantic function. Provide skill and function names, plus any variables the function requires.")]
    [OpenApiParameter(name: "skillName", Description = "Name of the skill e.g., 'FunSkill'", Required = true)]
    [OpenApiParameter(name: "functionName", Description = "Name of the function e.g., 'Excuses'", Required = true)]
    [OpenApiRequestBody("application/json", typeof(ExecuteFunctionRequest), Description = "Variables to use when executing the specified function.", Required = true)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ExecuteFunctionResponse), Description = "Returns the response from the AI.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Returned if the request body is invalid.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(ErrorResponse), Description = "Returned if the semantic function could not be found.")]
    public async Task<HttpResponseData> ExecuteFunctionAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "skills/{skillName}/functions/{functionName}")]
        HttpRequestData requestData,
        FunctionContext executionContext, string skillName, string functionName)
    {
        try
        {
            var functionRequest = await JsonSerializer.DeserializeAsync<ExecuteFunctionRequest>(requestData.Body, s_jsonOptions).ConfigureAwait(false);

            var skillConfig = SemanticFunctionConfig.ForSkillAndFunction(skillName, functionName);
            var function = _kernel.CreateSemanticFunction(skillConfig.PromptTemplate, skillConfig.Name, skillConfig.SkillName,
                                                       skillConfig.Description, skillConfig.MaxTokens, skillConfig.Temperature,
                                                       skillConfig.TopP, skillConfig.PPenalty, skillConfig.FPenalty);

            var context = new ContextVariables();
            foreach (var v in functionRequest.Variables)
            {
                context.Set(v.Key, v.Value);
            }

            var result = await this._kernel.RunAsync(context, function).ConfigureAwait(false);

            return await CreateResponseAsync(requestData, HttpStatusCode.OK, new ExecuteFunctionResponse() { Response = result.ToString() }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log the contents of the request
            var requestBody = await new StreamReader(requestData.Body).ReadToEndAsync();
            Console.WriteLine($"Failed to deserialize request body: {requestBody}. Exception: {ex}");

            return await CreateResponseAsync(requestData, HttpStatusCode.BadRequest, new ErrorResponse() { Message = $"Invalid request body." }).ConfigureAwait(false);
        }
    }
    
    [Function("GithubWebhook")]
     public async Task<HttpResponseData> GithubWebhook(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "github")]
        HttpRequestData requestData,
        FunctionContext executionContext)
    {
        
        var ghClient = await GetGitHubClient();
        var issueCommentResponse = await ghClient.Issue.Comment.Create("sk-dev-team", "issues", 1, "Hello from my GitHubApp Installation!");
        //var issueEvent = await JsonSerializer.DeserializeAsync<IssueEvent>(requestData.Body, s_jsonOptions).ConfigureAwait(false);
        
        // TODO:
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
            
            
        //     var skillConfig = SemanticFunctionConfig.ForSkillAndFunction(skillName, functionName);
        //     var function = _kernel.CreateSemanticFunction(skillConfig.PromptTemplate, skillConfig.Name, skillConfig.SkillName,
        //                                                skillConfig.Description, skillConfig.MaxTokens, skillConfig.Temperature,
        //                                                skillConfig.TopP, skillConfig.PPenalty, skillConfig.FPenalty);

        //     var context = new ContextVariables();
        //     foreach (var v in functionRequest.Variables)
        //     {
        //         context.Set(v.Key, v.Value);
        //     }

        //     var result = await this._kernel.RunAsync(context, function).ConfigureAwait(false);

        //     return await CreateResponseAsync(requestData, HttpStatusCode.OK, new ExecuteFunctionResponse() { Response = result.ToString() }).ConfigureAwait(false);
        // }
        return await CreateResponseAsync(requestData,HttpStatusCode.OK, new ExecuteFunctionResponse() { Response = JsonSerializer.Serialize(issueEvent) }).ConfigureAwait(false);
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

    private static async Task<HttpResponseData> CreateResponseAsync(HttpRequestData requestData, HttpStatusCode statusCode, object responseBody)
    {
        var responseData = requestData.CreateResponse(statusCode);
        await responseData.WriteAsJsonAsync(responseBody).ConfigureAwait(false);
        return responseData;
    }
}
