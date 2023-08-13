using System.Text;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Resources;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Octokit;
using Octokit.Helpers;

namespace SK.DevTeam
{
    public static class PullRequestActivities
    {
        [Function(nameof(SaveOutput))]
        public static async Task<bool> SaveOutput([ActivityTrigger] SaveOutputRequest request, FunctionContext executionContext)
        {
            var connectionString = Environment.GetEnvironmentVariable("SHARE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
            var shareName = Environment.GetEnvironmentVariable("SHARE_NAME", EnvironmentVariableTarget.Process);
            
            var parentDirName = $"{request.Directory}/{request.IssueOrchestrationId}";
            var fileName = $"{request.FileName}.{request.Extension}";

            var share = new ShareClient(connectionString, shareName);
            await share.CreateIfNotExistsAsync();

            var parentDir = share.GetDirectoryClient(parentDirName);
            await parentDir.CreateIfNotExistsAsync();

            var directory = parentDir.GetSubdirectoryClient(request.SubOrchestrationId);
            await directory.CreateIfNotExistsAsync();
            
            var file = directory.GetFileClient(fileName);
            // hack to enable script to save files in the same directory
            var cwdHack = "#!/bin/bash\n cd $(dirname $0)";
            var output = request.Extension == "sh"? request.Output.Replace("#!/bin/bash",cwdHack): request.Output;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(output)))
            {
                await file.CreateAsync(stream.Length);
                await file.UploadRangeAsync(
                    new HttpRange(0, stream.Length),
                    stream);
            }
            
            return true;
        }

        [Function(nameof(CreateBranch))]
        public static async Task<bool> CreateBranch([ActivityTrigger] GHNewBranch request, FunctionContext executionContext)
        {
            var ghClient = await GithubService.GetGitHubClient();
            var repo = await ghClient.Repository.Get(request.Org, request.Repo);
            await ghClient.Git.Reference.CreateBranch(request.Org, request.Repo, request.Branch, repo.DefaultBranch);
            return true;
        }

        [Function(nameof(CreatePR))]
        public static async Task<bool> CreatePR([ActivityTrigger] GHNewBranch request, FunctionContext executionContext)
        {
            var ghClient = await GithubService.GetGitHubClient();
            var repo = await ghClient.Repository.Get(request.Org, request.Repo);
            await ghClient.PullRequest.Create(request.Org, request.Repo, new NewPullRequest($"New app #{request.Number}", request.Branch, repo.DefaultBranch));
            return true;
        }

        [Function(nameof(RunInSandbox))]
        public static async Task<bool> RunInSandbox([ActivityTrigger] AddToPRRequest request, FunctionContext executionContext)
        {
             var client = new ArmClient(new DefaultAzureCredential());

            var subscriptionId =  Environment.GetEnvironmentVariable("AZURE_SUB", EnvironmentVariableTarget.Process);
            var resourceGroupName = Environment.GetEnvironmentVariable("AZURE_CG_RG", EnvironmentVariableTarget.Process);
            var containerGroupName = $"sk-sandbox-{request.SubOrchestrationId}";
            var containerName =  $"sk-sandbox-{request.SubOrchestrationId}";
            var shareName = Environment.GetEnvironmentVariable("SHARE_NAME", EnvironmentVariableTarget.Process);
            var accountName = Environment.GetEnvironmentVariable("SHARE_ACCOUNT_NAME", EnvironmentVariableTarget.Process);
            var shareKey = Environment.GetEnvironmentVariable("SHARE_KEY", EnvironmentVariableTarget.Process);
            var azLocation = Environment.GetEnvironmentVariable("AZURE_LOCATION", EnvironmentVariableTarget.Process);
            var image = Environment.GetEnvironmentVariable("SANDBOX_IMAGE", EnvironmentVariableTarget.Process);

            var resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
            var resourceGroupResource = client.GetResourceGroupResource(resourceGroupResourceId);

            var scriptPath = $"/azfiles/output/{request.IssueOrchestrationId}/{request.SubOrchestrationId}/run.sh";

            var collection = resourceGroupResource.GetContainerGroups();
            
            var data = new ContainerGroupData(new AzureLocation(azLocation), new ContainerInstanceContainer[]
            {
                    new ContainerInstanceContainer(containerName,image,new ContainerResourceRequirements(new ContainerResourceRequestsContent(1.5,1)))
                    {
                        Command = { "/bin/bash", $"{scriptPath}" },
                        VolumeMounts =
                        {
                            new ContainerVolumeMount("azfiles","/azfiles/")
                            {
                                IsReadOnly = false,
                            }
                        },
                    }}, ContainerInstanceOperatingSystemType.Linux)
                                {
                                    Volumes =
                                    {
                                        new ContainerVolume("azfiles")
                                        {
                                            AzureFile = new ContainerInstanceAzureFileVolume(shareName,accountName)
                                            {
                                                StorageAccountKey = shareKey
                                            },
                                        },
                                    },
                                    RestartPolicy = ContainerGroupRestartPolicy.Never,
                                    Sku = ContainerGroupSku.Standard,
                                    Priority = ContainerGroupPriority.Regular
                                };
            await collection.CreateOrUpdateAsync(WaitUntil.Completed, containerGroupName, data);
            // TODO: schedule containerGroup for deletion (separate az function)
            return true;
        }

        [Function(nameof(CommitToGithub))]
        public static async Task<bool> CommitToGithub([ActivityTrigger] GHCommitRequest request, FunctionContext executionContext)
        {
            var connectionString = Environment.GetEnvironmentVariable("SHARE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
            var shareName = Environment.GetEnvironmentVariable("SHARE_NAME", EnvironmentVariableTarget.Process);
            var ghClient = await GithubService.GetGitHubClient();
            
            var dirName = $"{request.Directory}/{request.IssueOrchestrationId}/{request.SubOrchestrationId}";
            var share = new ShareClient(connectionString, shareName);
            var directory = share.GetDirectoryClient(dirName);

            var remaining = new Queue<ShareDirectoryClient>();
            remaining.Enqueue(directory);
            while (remaining.Count > 0)
            {
                var dir = remaining.Dequeue();
                
                await foreach (var item in dir.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory && item.Name != "run.sh") // we don't want the generated script in the PR
                    {
                        var file = dir.GetFileClient(item.Name);
                        var filePath = file.Path.Replace($"{shareName}/", "")
                                                .Replace($"{dirName}/", "");
                        var fileStream = await file.OpenReadAsync();
                        using (var reader = new StreamReader(fileStream, Encoding.UTF8))
                        {
                            var value = reader.ReadToEnd();
                            
                            await ghClient.Repository.Content.CreateFile(
                                    request.Org, request.Repo, filePath,
                                    new CreateFileRequest($"Commit message", value, request.Branch)); // TODO: add more meaningfull commit message
                        }
                    }
                    else
                        remaining.Enqueue(dir.GetSubdirectoryClient(item.Name));
                }
            }
           
            return true;
        }
    }
}