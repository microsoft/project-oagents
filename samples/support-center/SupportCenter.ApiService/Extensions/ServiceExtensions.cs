using SupportCenter.ApiService.Data.CosmosDb;
using SupportCenter.ApiService.Options;
using SupportCenter.ApiService.SemanticKernel.Plugins.CustomerPlugin;
using static SupportCenter.ApiService.SemanticKernel.Extensions;

namespace SupportCenter.ApiService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ExtendOptions(this IServiceCollection services)
        {
            services.AddOptions<OpenAIOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(OpenAIOptions)).Bind(settings);
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<QdrantOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(QdrantOptions)).Bind(settings);
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<CosmosDbOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(CosmosDbOptions)).Bind(settings);
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<AISearchOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection(nameof(AISearchOptions)).Bind(settings);
                })
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public static IServiceCollection ExtendServices(this IServiceCollection services)
        {
            RegisterRepositories(services);
            AddSemanticKernelResolvers(services);
            AddSemanticKernelServices(services);
            return services;
        }

        private static void AddSemanticKernelServices(IServiceCollection services)
        {
            services.AddKeyedSingleton("ConversationKernel", (sp, _) => CreateKernel(sp, "Conversation"));
            services.AddKeyedSingleton("CustomerInfoKernel", (sp, _) => CreateKernel(sp, "CustomerInfo"));
            services.AddKeyedSingleton("DispatcherKernel", (sp, _) => CreateKernel(sp, "Dispatcher"));
            services.AddKeyedSingleton("InvoiceKernel", (sp, _) => CreateKernel(sp, "Invoice"));
            services.AddKeyedSingleton("DiscountKernel", (sp, _) => CreateKernel(sp, "Discount"));
            services.AddKeyedSingleton("QnAKernel", (sp, _) => CreateKernel(sp, "QnA"));

            services.AddKeyedSingleton("ConversationMemory", (sp, _) => CreateMemory(sp, "Conversation"));
            services.AddKeyedSingleton("CustomerInfoMemory", (sp, _) => CreateMemory(sp, "CustomerInfo"));
            services.AddKeyedSingleton("DispatcherMemory", (sp, _) => CreateMemory(sp, "Dispatcher"));
            services.AddKeyedSingleton("InvoiceMemory", (sp, _) => CreateMemory(sp, "Invoice"));
            services.AddKeyedSingleton("DiscountMemory", (sp, _) => CreateMemory(sp, "Discount"));
            services.AddKeyedSingleton("QnAMemory", (sp, _) => CreateMemory(sp, "QnA"));
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddSingleton<ICustomerRepository, CustomerRepository>();
        }

        private static void AddSemanticKernelResolvers(IServiceCollection services)
        {
            /* 
             * Register the resolvers for the Semantic Kernel
             * This is used to resolve the kernel and memory for the agent
             * The kernel is used to execute the functions and the memory is used to store the state
             */
            //services.AddSingleton<KernelResolver>(serviceProvider => agent =>
            //{
            //    return CreateKernel(serviceProvider, agent);
            //});
            //services.AddSingleton<SemanticTextMemoryResolver>(serviceProvider => agent =>
            //{
            //    return CreateMemory(serviceProvider, agent);
            //});
        }

        public static void RegisterSemanticKernelNativeFunctions(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CustomerData>();
        }
    }
}
