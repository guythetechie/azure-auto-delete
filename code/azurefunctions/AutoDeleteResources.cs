using Azure;
using Microsoft.Azure.Functions.Worker;
using System.Threading;
using System.Threading.Tasks;

namespace azurefunctions;

public class AutoDeleteResources
{
    private readonly ListResourcesByTag listResources;

    public AutoDeleteResources(ListResourcesByTag listResources)
    {
        this.listResources = listResources;
    }

    [Function("delete-tagged-resources")]
#pragma warning disable IDE0060 // Remove unused parameter
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo trigger, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var tagName = TagName.New("auto-delete");
        var tagValue = TagValue.New("yes");

        await listResources(tagName, tagValue, cancellationToken)
                .Iter(async resource => await resource.DeleteAsync(WaitUntil.Started, cancellationToken), cancellationToken);
    }
}
