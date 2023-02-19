using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

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
                .AddSingleton(GetArmClient);
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
}