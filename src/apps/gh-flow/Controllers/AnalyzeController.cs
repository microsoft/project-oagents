using Microsoft.AI.DevTeam;
using Microsoft.AspNetCore.Mvc;
using Octokit;

[ApiController]
public class AnalyzeController : ControllerBase
{
    private readonly GithubService _githubService;

    public AnalyzeController(GithubService githubService)
    {
       _githubService=githubService;
    }

    [HttpPost("analyze")]
    public async Task<string> Analyze([FromBody] AnalyzeRepoRequest request)
    {
        var mainLanguage = await _githubService.GetMainLanguage(request.Org, request.Repo);
        var files = await _githubService.GetFiles(request.Org, request.Repo, "", Language.Filters[mainLanguage]);
        // TODO: send each file to be analyzed
        return "";
    }
}

// TODO: add more languages
public static class Language
{
    public static Dictionary<string, Func<RepositoryContent, bool>> Filters = new Dictionary<string, Func<RepositoryContent, bool>> {
        {"C#", f => f.Name.EndsWith(".cs") }
    };
}

public class AnalyzeRepoRequest
{
    public string Org { get; set; }
    public string Repo { get; set; }
}
