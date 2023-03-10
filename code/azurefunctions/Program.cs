using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace azurefunctions;

public static class Program
{
    private static async Task Main(string[] arguments)
    {
        await new HostBuilder().ConfigureFunctionsWorkerDefaults(ConfigureFunctionsWorker)
                               .ConfigureLogging(AddDebugLogging)
                               .ConfigureServices(ConfigureServices)
                               .Build()
                               .RunAsync();
    }

    private static void ConfigureFunctionsWorker(IFunctionsWorkerApplicationBuilder builder)
    {
        builder.AddApplicationInsights()
               .AddApplicationInsightsLogger();
    }

    private static void AddDebugLogging(ILoggingBuilder builder)
    {
        builder.AddConsole();
        builder.AddDebug();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.RemoveLoggingFilter()
                .AddSingleton(GetTokenCredential)
                .AddSingleton(GetArmClient)
                .AddSingleton(ListResourcesByTag)
                .AddSingleton(DeleteArmResource);
    }

    private static IServiceCollection RemoveLoggingFilter(this IServiceCollection services)
    {
        services.Configure<LoggerFilterOptions>(options =>
        {
            var rulesToRemove = options.Rules.Where(rule => rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider")
                                             .ToList();

            foreach (var rule in rulesToRemove)
            {
                options.Rules.Remove(rule);
            }
        });

        return services;
    }

    private static TokenCredential GetTokenCredential(IServiceProvider provider)
    {
        return new DefaultAzureCredential();
    }

    private static ArmClient GetArmClient(IServiceProvider provider)
    {
        var credential = provider.GetRequiredService<TokenCredential>();
        return new ArmClient(credential);
    }

    private static ListArmResourcesByTag ListResourcesByTag(IServiceProvider provider)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(ListResourcesByTag));
        var armClient = provider.GetRequiredService<ArmClient>();

        return (tagName, tagValue, token) =>
        {
            logger.LogInformation("Finding resources with tag name {TagName} and value {TagValue}...", tagName.Value, tagValue.Value);

            var resources = Azure.ListResources(armClient, tagName, tagValue, token)
                                 .Select(resource => resource as ArmResource);

            var resourceGroups = Azure.ListResourceGroups(armClient, tagName, tagValue, token)
                                      .Select(resourceGroup => resourceGroup as ArmResource);

            return resources.Concat(resourceGroups)
                            .Select(resource =>
                            {
                                logger.LogInformation("Found resource with ID {ResourceID}...", resource.Id);
                                return resource;
                            });
        };
    }

    private static DeleteArmResource DeleteArmResource(IServiceProvider provider)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(ListResourcesByTag));
        var armClient = provider.GetRequiredService<ArmClient>();

        return async (resource, token) =>
        {
            logger.LogInformation("Deleting resource with ID {ResourceID}...", resource.Id);
            await (resource switch
            {
                GenericResource genericResource => genericResource.DeleteAsync(WaitUntil.Started, token),
                ResourceGroupResource resourceGroupResource => resourceGroupResource.DeleteAsync(WaitUntil.Started, cancellationToken: token),
                _ => throw new NotImplementedException()
            });

            return Prelude.unit;
        };
    }
}