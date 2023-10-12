using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;

public class SandboxSkill
{
    public Task<string> RunInAlpineAsync(string input)
    {
        return RunInContainer(input, "alpine:3.18");
    }

    public Task<string> RunInDotnetAlpineAsync(string input)
    {
        return RunInContainer(input, "mcr.microsoft.com/dotnet/sdk:7.0-alpine");
    }

    private static async Task<string> RunInContainer(string input, string image)
    {
        var tempScriptFile = Path.ChangeExtension(Guid.NewGuid().ToString(), "sh");

        var srcDirectoryPath = Path.Combine(Directory.GetCurrentDirectory(), "output", "src");
        _ = Directory.CreateDirectory(srcDirectoryPath);

        var dotnetContainer = new ContainerBuilder()
            .WithImage(image)
            .WithBindMount(srcDirectoryPath, "/src")
            .WithResourceMapping(Encoding.Default.GetBytes(input), $"/src/{tempScriptFile}")
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new ScriptCompleted()))
            .WithWorkingDirectory("/src")
            .WithEntrypoint("/bin/sh")
            .WithCommand(tempScriptFile)
            .Build();

        await dotnetContainer.StartAsync()
            .ConfigureAwait(false);

        File.Delete(Path.Combine(srcDirectoryPath, tempScriptFile));
        return string.Empty;
    }

    private sealed class ScriptCompleted : IWaitUntil
    {
        public Task<bool> UntilAsync(IContainer container)
        {
            return Task.FromResult(TestcontainersStates.Exited.Equals(container.State));
        }
    }
}