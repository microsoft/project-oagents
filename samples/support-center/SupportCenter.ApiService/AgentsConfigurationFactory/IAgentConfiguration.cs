using Microsoft.SemanticKernel;
using SupportCenter.ApiService.Options;

namespace SupportCenter.ApiService.AgentsConfigurationFactory
{
    public interface IAgentConfiguration
    {
        void ConfigureOpenAI(OpenAIOptions options);
        void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider);
    }
}
