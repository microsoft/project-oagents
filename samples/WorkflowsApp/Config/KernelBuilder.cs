using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;

internal static class KernelBuilder
{    
    /// <summary>
    /// Gets a semantic kernel instance
    /// </summary>
    /// <returns>Microsoft.SemanticKernel.IKernel</returns>
    public static Kernel BuildKernel()
    {
        var kernelSettings = KernelSettings.LoadSettings();

        var clientOptions = new OpenAIClientOptions();
        
        clientOptions.Retry.NetworkTimeout = TimeSpan.FromMinutes(5);
        
        var openAIClient = new OpenAIClient(new Uri(kernelSettings.Endpoint), new AzureKeyCredential(kernelSettings.ApiKey), clientOptions);
        var builder = Kernel.CreateBuilder();
        
        builder.Services.AddLogging(c => c.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug));

        if (kernelSettings.ServiceType.Equals("AZUREOPENAI", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddAzureOpenAIChatCompletion(kernelSettings.DeploymentOrModelId, openAIClient);
        }
        else
        {
            builder.Services.AddOpenAIChatCompletion(kernelSettings.DeploymentOrModelId, kernelSettings.ApiKey);
        }

        builder.Services.ConfigureHttpClientDefaults(c =>
        {
            c.AddStandardResilienceHandler().Configure(o =>
            {
                o.Retry.MaxRetryAttempts = 5;
                o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            });
        });

        return builder.Build();
    }
}
