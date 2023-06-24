using Octokit;

public static class GithubService
{
    public static async Task<GitHubClient> GetGitHubClient()
    {
            var key = Environment.GetEnvironmentVariable("GH_APP_KEY", EnvironmentVariableTarget.Process);
            var appId = int.Parse(Environment.GetEnvironmentVariable("GH_APP_ID", EnvironmentVariableTarget.Process));
            var installationId = int.Parse(Environment.GetEnvironmentVariable("GH_INST_ID", EnvironmentVariableTarget.Process));

            // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
            var generator = new GitHubJwt.GitHubJwtFactory(
                new GitHubJwt.StringPrivateKeySource(key),
                new GitHubJwt.GitHubJwtFactoryOptions
                {
                    AppIntegrationId = appId, // The GitHub App Id
                    ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
                }
            );

            var jwtToken = generator.CreateEncodedJwtToken();
            var appClient = new GitHubClient(new ProductHeaderValue("SK-DEV-APP"))
            {
                Credentials = new Credentials(jwtToken, AuthenticationType.Bearer)
            };
            var response = await appClient.GitHubApps.CreateInstallationToken(installationId);
            return new GitHubClient(new ProductHeaderValue($"SK-DEV-APP-Installation{installationId}"))
            {
                Credentials = new Credentials(response.Token)
            };
    }
}