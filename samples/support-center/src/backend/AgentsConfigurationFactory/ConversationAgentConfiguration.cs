using Microsoft.SemanticKernel;
using SupportCenter.Options;

namespace SupportCenter.AgentsConfigurationFactory
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