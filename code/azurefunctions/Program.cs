using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.Design;
using System.Linq;

namespace azurefunctions;

internal static class Program
{
    private static void Main(string[] arguments)
    {
        new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices(ConfigureServices)
            .Build()
            .Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(GetTokenCredential)
                .AddSingleton(GetArmClient)
                .AddSingleton(ListResourcesByTag);
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

    private static ListResourcesByTag ListResourcesByTag(IServiceProvider provider)
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(ListResourcesByTag));
        var armClient = provider.GetRequiredService<ArmClient>();

        return (tagName, tagValue, token) =>
        {
            logger.LogInformation("Finding resources with tag name {TagName} and value {TagValue}...", tagName.Value, tagValue.Value);

            return AzureResource.List(armClient, tagName, tagValue, token)
                    .Select(resource =>
                    {
                        logger.LogInformation("Processing resource with ID {ResourceID}...", resource.Id);
                        return resource;
                    });
        };
    }
}