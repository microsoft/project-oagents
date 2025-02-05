﻿using Azure.AI.OpenAI;
using Azure;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using SupportCenter.ApiService.Extensions;
using SupportCenter.ApiService.AgentsConfigurationFactory;
using SupportCenter.ApiService.Options;
using OpenAI;

namespace SupportCenter.ApiService.SemanticKernel
{
    public static class Extensions
    {
        public static ISemanticTextMemory CreateMemory(IServiceProvider provider, string agent)
        {
            OpenAIOptions openAiConfig = provider.GetService<IOptions<OpenAIOptions>>()?.Value ?? new OpenAIOptions();
            openAiConfig.ValidateRequiredProperties();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddConsole()
                    .AddDebug();
            });

            if (agent == "Invoice")
            {
                AISearchOptions aiSearchConfig = provider.GetService<IOptions<AISearchOptions>>()?.Value ?? new AISearchOptions();
                aiSearchConfig.ValidateRequiredProperties();

                var memoryBuilder = new MemoryBuilder();
                return memoryBuilder.WithLoggerFactory(loggerFactory)
                                .WithMemoryStore(new AzureAISearchMemoryStore(aiSearchConfig.SearchEndpoint, aiSearchConfig.SearchKey))
                                //.WithAzureOpenAITextEmbeddingGeneration(aiSearchConfig.SearchEmbeddingDeploymentOrModelId, aiSearchConfig.SearchEmbeddingEndpoint, aiSearchConfig.SearchEmbeddingApiKey)
                                .Build();
            }
            else
            {
                QdrantOptions qdrantConfig = provider.GetService<IOptions<QdrantOptions>>()?.Value ?? new QdrantOptions();
                qdrantConfig.ValidateRequiredProperties();

                return new MemoryBuilder().WithLoggerFactory(loggerFactory)
                             .WithQdrantMemoryStore(qdrantConfig.Endpoint, qdrantConfig.VectorSize)
                             //.WithAzureOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingsDeploymentOrModelId, openAiConfig.EmbeddingsEndpoint, openAiConfig.EmbeddingsApiKey)
                             .Build();
            }
        }

        public static Kernel CreateKernel(IServiceProvider provider, string agent)
        {
            var client = provider.GetService<OpenAIClient>();

            var agentConfiguration = AgentConfiguration.GetAgentConfiguration(agent);
            //agentConfiguration.ConfigureOpenAI(openAiConfig);

            //var clientOptions = new AzureOpenAIClientOptions();
            //clientOptions.Retry.NetworkTimeout = TimeSpan.FromMinutes(5);
            var builder = Kernel.CreateBuilder();
            //builder.AddAzureOpenAIChatCompletion("", client);
            //builder.AddAzureOpenAIChatCompletion(client);
            //builder.Services.AddLogging(c => c.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug));

            //// Chat
            //if (openAiConfig.ChatEndpoint.Contains(".azure", StringComparison.OrdinalIgnoreCase))
            //{
            //    builder.Services.AddAzureOpenAIChatCompletion(openAiConfig.ChatDeploymentOrModelId, openAIClient);
            //}
            //else
            //{
            //    builder.Services.AddOpenAIChatCompletion(openAiConfig.ChatDeploymentOrModelId, openAIClient);
            //}
            //// Embeddings
            //openAIClient = new AzureOpenAIClient(new Uri(openAiConfig.EmbeddingsEndpoint), new AzureKeyCredential(openAiConfig.EmbeddingsApiKey), clientOptions);
            //if (openAiConfig.EmbeddingsEndpoint.Contains(".azure", StringComparison.OrdinalIgnoreCase))
            //{
            //    builder.Services.AddAzureOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingsDeploymentOrModelId, openAIClient);
            //}
            //else
            //{
            //    builder.Services.AddOpenAITextEmbeddingGeneration(openAiConfig.EmbeddingsDeploymentOrModelId, openAIClient);
            //}

            //builder.Services.ConfigureHttpClientDefaults(c =>
            //{
            //    c.AddStandardResilienceHandler().Configure(o =>
            //    {
            //        o.Retry.MaxRetryAttempts = 5;
            //        o.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            //    });
            //});

            var kernel = builder.Build();
            agentConfiguration.ConfigureKernel(kernel, provider);

            return kernel;
        }
    }
}
