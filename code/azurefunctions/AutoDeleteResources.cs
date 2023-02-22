using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using System.Threading;
using System.Threading.Tasks;

namespace azurefunctions;

public class AutoDeleteResources
{
    private readonly ListArmResourcesByTag listResources;
    private readonly DeleteArmResource deleteArmResource;

    public AutoDeleteResources(ListArmResourcesByTag listResources, DeleteArmResource deleteArmResource)
    {
        this.listResources = listResources;
        this.deleteArmResource = deleteArmResource;
    }

    [Function("delete-tagged-resources")]
#pragma warning disable IDE0060 // Remove unused parameter
    public async Task<Unit> Run([TimerTrigger("0 0 */1 * * *")] TimerInfo trigger, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        var tagName = TagName.New("auto-delete");
        var tagValue = TagValue.New("yes");

        return await listResources(tagName, tagValue, cancellationToken)
                        .Iter(async resource => await deleteArmResource(resource, cancellationToken), cancellationToken);
    }
}
