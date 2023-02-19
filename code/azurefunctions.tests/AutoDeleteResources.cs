using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Azure.Functions.Worker;
using Moq;
using System;
using System.Linq;
using System.Threading;

namespace azurefunctions.tests;

public class AutoDeleteResourcesTests
{
    [Property]
    public Property Resources_with_deletion_tags_are_deleted()
    {
        var deletionCount = 0;

        var arbitrary = Gen.Constant(GenerateGenericResource(onDelete: () => Interlocked.Increment(ref deletionCount)))
                           .ListOf()
                           .ToArbitrary();

        return Prop.ForAll(arbitrary, async resources =>
        {
            // Arrange
            ListResourcesByTag listResources = (tagName, tagValue, token) => resources.ToAsyncEnumerable();
            var cancellationToken = CancellationToken.None;

            // Act
            await new AutoDeleteResources(listResources).Run(new TimerInfo(), cancellationToken);

            // Assert
            deletionCount.Should().Be(resources.Count);
        });
    }

    private static GenericResource GenerateGenericResource(Action onDelete)
    {
        var mock = new Mock<GenericResource>(MockBehavior.Strict);

        mock.Setup(resource => resource.DeleteAsync(It.IsAny<WaitUntil>(), It.IsAny<CancellationToken>()).Result)
            .Callback(onDelete)
            .Returns(It.IsAny<ArmOperation>());

        return mock.Object;
    }
}