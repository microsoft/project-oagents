namespace Microsoft.AI.DevTeam.Dapr.Events
{
    public enum GithubFlowEventType
    {
        NewAsk,
        ReadmeChainClosed,
        CodeChainClosed,
        CodeGenerationRequested,
        DevPlanRequested,
        ReadmeGenerated,
        DevPlanGenerated,
        CodeGenerated,
        DevPlanChainClosed,
        ReadmeRequested,
        ReadmeStored,
        SandboxRunFinished,
        ReadmeCreated,
        CodeCreated,
        DevPlanCreated,
        SandboxRunCreated
    }
}