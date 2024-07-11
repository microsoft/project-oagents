using Microsoft.SemanticKernel;
using SupportCenter.Options;

namespace SupportCenter.AgentsConfigurationFactory
{
    internal class DiscountAgentConfiguration : IAgentConfiguration
    {
        public void Configure(OpenAIOptions options)
        {
        }

        public void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider)
        {
        }
    }
}