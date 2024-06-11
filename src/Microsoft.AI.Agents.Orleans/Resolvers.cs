using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel;

namespace Microsoft.AI.Agents.Orleans
{
    public class Resolvers
    {
        public delegate Kernel KernelResolver(string agent);
        public delegate ISemanticTextMemory SemanticTextMemoryResolver(string agent);
    }
}
