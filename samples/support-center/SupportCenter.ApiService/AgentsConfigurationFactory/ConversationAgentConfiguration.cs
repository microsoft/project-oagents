using Microsoft.SemanticKernel;
using SupportCenter.ApiService.Options;

namespace SupportCenter.ApiService.AgentsConfigurationFactory
{
    public class ConversationAgentConfiguration : IAgentConfiguration
    {
        public void ConfigureOpenAI(OpenAIOptions options)
        {
            options.ChatDeploymentOrModelId = options.ConversationDeploymentOrModelId ?? options.ChatDeploymentOrModelId;
        }

        public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
        {
        }
    }
}