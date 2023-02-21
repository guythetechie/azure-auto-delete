using Azure.Core;
using Azure.ResourceManager;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace azurefunctions.tests;

public class AutoDeleteResourcesTests
{
    [Property]
    public Property Resources_with_deletion_tags_are_deleted()
    {
        var arbitrary = Fixture.Generate().ToArbitrary();

        return Prop.ForAll(arbitrary, async fixture =>
        {
            // Act
            await RunFunction(fixture);

            // Assert
            var expectedResources = fixture.Resources.Map(resource => resource.Id.ToString());
            var actualResources = fixture.DeletedResources.Map(resource => resource.ToString());
            actualResources.Should().BeEquivalentTo(expectedResources);
        });
    }

    private sealed record Fixture
    {
        public Seq<ArmResource> Resources { get; init; }

        public AtomSeq<ResourceIdentifier> DeletedResources { get; init; } = AtomSeq<ResourceIdentifier>.Empty;

        public static Gen<Fixture> Generate()
        {
            return Gen.Constant(GenerateResource())
                      .ListOf()
                      .Select(list => new Fixture
                      {
                          Resources = list.ToSeq()
                      });
        }

        private static ArmResource GenerateResource()
        {
            var mock = new Mock<ArmResource>(MockBehavior.Strict);

            mock.Setup(resource => resource.Id)
                .Returns(new ResourceIdentifier(Guid.NewGuid().ToString()));

            return mock.Object;
        }
    }

    private static async ValueTask<Unit> RunFunction(Fixture fixture)
    {
        ListArmResourcesByTag listResources = (tagName, tagValue, token) => fixture.Resources.ToAsyncEnumerable();

        DeleteArmResource deleteResource = async (resource, token) =>
        {
            await ValueTask.CompletedTask;
            return fixture.DeletedResources.Add(resource.Id);
        };

        return await new AutoDeleteResources(listResources, deleteResource)
                            .Run(new TimerInfo(), CancellationToken.None);
    }
}