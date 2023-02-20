using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Azure.Functions.Worker;
using Moq;
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
            var resourceCount = fixture.Resources.Count;
            var deletionCount = fixture.DeletionCount;
            deletionCount.Should().Be(resourceCount);
        });
    }

    private sealed record Fixture
    {
        private int deletionCount;

        public Seq<GenericResource> Resources { get; private set; } = Seq<GenericResource>.Empty;

        public ListResourcesByTag ListResourcesByTag => (tagName, tagValue, token) => Resources.ToAsyncEnumerable();

        public CancellationToken CancellationToken { get; } = CancellationToken.None;

        public int DeletionCount => deletionCount;

        public static Gen<Fixture> Generate()
        {
            var fixture = new Fixture();

            return Gen.Constant(fixture.GenerateResource())
                      .ListOf()
                      .Select(resources =>
                      {
                          fixture.Resources = resources.ToSeq();
                          return fixture;
                      });
        }

        private GenericResource GenerateResource()
        {
            var mock = new Mock<GenericResource>(MockBehavior.Strict);

            mock.Setup(resource => resource.DeleteAsync(It.IsAny<WaitUntil>(), It.IsAny<CancellationToken>()).Result)
                .Callback(() => Interlocked.Increment(ref deletionCount))
                .Returns(It.IsAny<ArmOperation>());

            return mock.Object;
        }
    }

    private static async ValueTask RunFunction(Fixture fixture)
    {
        await new AutoDeleteResources(fixture.ListResourcesByTag)
                    .Run(new TimerInfo(), fixture.CancellationToken);
    }
}