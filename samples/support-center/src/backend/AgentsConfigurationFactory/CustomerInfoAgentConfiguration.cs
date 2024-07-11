using Microsoft.SemanticKernel;
using SupportCenter.Options;
using SupportCenter.SemanticKernel.Plugins.CustomerPlugin;

namespace SupportCenter.AgentsConfigurationFactory
{
    public class CustomerInfoAgentConfiguration : IAgentConfiguration
    {
        private readonly string customerPlugin = "CustomerPlugin";

        public void ConfigureOpenAI(OpenAIOptions options)
        {
            options.ChatDeploymentOrModelId = options.ConversationDeploymentOrModelId ?? options.ChatDeploymentOrModelId;
        }

        public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
        {
            if (kernel.Plugins.TryGetPlugin(customerPlugin, out _) == false)
                kernel.ImportPluginFromObject(serviceProvider.GetRequiredService<CustomerData>(), customerPlugin);
        }
    }
}