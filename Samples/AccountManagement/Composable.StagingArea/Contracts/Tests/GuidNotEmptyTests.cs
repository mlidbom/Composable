using System;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class GuidNotEmptyTests
    {
        [Test]
        public void NotEmptyThrowsArgumentExceptionForEmptyGuid()
        {
            var emptyGuid = Guid.Empty;
            var aGuid = Guid.NewGuid();

            Assert.Throws<GuidIsEmptyException>(() => Contract.Optimized.Arguments(emptyGuid).NotEmpty());
            Assert.Throws<GuidIsEmptyException>(() => Contract.Optimized.Arguments(aGuid, emptyGuid).NotEmpty());
            Assert.Throws<GuidIsEmptyException>(() => Contract.Optimized.Argument(emptyGuid, "emptyGuid").NotEmpty())
                .Message.Should().Contain("emptyGuid");

            Assert.Throws<GuidIsEmptyException>(() => Contract.Arguments(() => emptyGuid).NotEmpty());
            Assert.Throws<GuidIsEmptyException>(() => Contract.Arguments(() => emptyGuid).NotEmpty())
                .Message.Should().Contain("emptyGuid");

            Assert.Throws<GuidIsEmptyException>(() => Contract.Arguments(() => aGuid, () => emptyGuid).NotEmpty())
                .Message.Should().Contain("emptyGuid");
        }

        [Test]
        public void NotEmptyThrowsArgumentExceptionForEmptyGuidNew()
        {
            InspectionTestHelper.BatchTestInspection<GuidIsEmptyException, Guid>(
                assert: inspected => inspected.NotEmpty(),
                badValues: new Guid[] {Guid.Empty, new Guid()},
                goodValues: new Guid[] {Guid.NewGuid(), Guid.NewGuid()});
        }
    }
}
