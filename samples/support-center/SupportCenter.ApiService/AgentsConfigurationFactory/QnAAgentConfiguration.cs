using Microsoft.SemanticKernel;
using SupportCenter.ApiService.Options;

namespace SupportCenter.ApiService.AgentsConfigurationFactory
{
    internal class QnAAgentConfiguration : IAgentConfiguration
    {
        public void ConfigureOpenAI(OpenAIOptions options)
        {
        }

        public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
        {
        }
    }
}