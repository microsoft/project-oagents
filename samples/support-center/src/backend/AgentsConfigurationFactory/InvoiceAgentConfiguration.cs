using Microsoft.SemanticKernel;
using SupportCenter.Options;

namespace SupportCenter.AgentsConfigurationFactory
{
    public class InvoiceAgentConfiguration : IAgentConfiguration
    {
        public void Configure(OpenAIOptions options)
        {
            options.ChatDeploymentOrModelId = options.InvoiceDeploymentOrModelId ?? options.ChatDeploymentOrModelId;
        }

        public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
        {
        }
    }
}
