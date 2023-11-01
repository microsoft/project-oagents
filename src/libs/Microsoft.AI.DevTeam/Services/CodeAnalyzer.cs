using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Octokit.Internal;

namespace Microsoft.AI.DevTeam;

public interface IAnalyzeCode 
{
    Task<CodeAnalysis> Analyze(string content);
}
public class CodeAnalyzer : IAnalyzeCode
{
    private readonly ServiceOptions _serviceOptions;
    private readonly HttpClient _httpClient;

    public CodeAnalyzer(IOptions<ServiceOptions> serviceOptions, HttpClient httpClient)
    {
        _serviceOptions = serviceOptions.Value;
        _httpClient = httpClient;
        
    }
    public async Task<CodeAnalysis> Analyze(string content)
    {
        _httpClient.BaseAddress = new Uri(_serviceOptions.IngesterUrl);
        var request = new CodeAnalysisRequest { Content = content };
        var response = await _httpClient.PostAsJsonAsync("AnalyzeCode", request);
        return await response.Content.ReadFromJsonAsync<CodeAnalysis>();
    }
}

public class CodeAnalysisRequest
{
    public string Content { get; set; }
}

public class CodeAnalysis
{
    public string Meaning { get; set; }
    public string CodeBlock { get; set; }
}
