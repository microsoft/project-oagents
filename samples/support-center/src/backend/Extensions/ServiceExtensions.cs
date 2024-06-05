using SupportCenter.Data.CosmosDb;
using SupportCenter.Options;
using SupportCenter.Plugins.CustomerPlugin;

namespace SupportCenter.Extensions
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
            return services;
        }

        private static void RegisterRepositories(IServiceCollection services)
        {
            services.AddSingleton<ICustomerRepository, CustomerRepository>();
        }

        public static void RegisterNativeFunctions(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<CustomerData>();

        }
    }
}
