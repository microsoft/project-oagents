using Microsoft.SemanticKernel;
using SupportCenter.Options;

namespace SupportCenter.AgentsConfigurationFactory
{
    public interface IAgentConfiguration
    {
        void Configure(OpenAIOptions options);
        void ConfigureKernel(Kernel kernel, IServiceProvider serviceProvider);
    }
}
