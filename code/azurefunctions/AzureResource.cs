using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using LanguageExt;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace azurefunctions;

public class TagName : NewType<TagName, string>
{
    public TagName(string value) : base(value) { }
}

public class TagValue : NewType<TagValue, string>
{
    public TagValue(string value) : base(value) { }
}

public delegate IAsyncEnumerable<GenericResource> ListResourcesByTag(TagName tagName, TagValue tagValue, CancellationToken cancellationToken);

internal static class AzureResource
{
    public static async IAsyncEnumerable<GenericResource> List(ArmClient client, TagName tagName, TagValue tagValue, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscription = await client.GetDefaultSubscriptionAsync(cancellationToken);
        var resources = subscription.GetGenericResourcesAsync(filter: $"tagName eq '{tagName.Value}' and tagValue eq '{tagValue.Value}'", cancellationToken: cancellationToken);

        await foreach (var resource in resources.WithCancellation(cancellationToken))
        {
            yield return resource;
        }
    }
}
