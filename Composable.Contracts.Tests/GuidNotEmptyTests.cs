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

            Assert.Throws<GuidIsEmptyContractViolationException>(() => Contract.ReturnValue(emptyGuid).NotEmpty())
                .Message.Should().Contain("ReturnValue");

            Assert.Throws<GuidIsEmptyContractViolationException>(() => Contract.Arguments(() => emptyGuid).NotEmpty());
            Assert.Throws<GuidIsEmptyContractViolationException>(() => Contract.Arguments(() => emptyGuid).NotEmpty())
                .Message.Should().Contain("emptyGuid");

            Assert.Throws<GuidIsEmptyContractViolationException>(() => Contract.Arguments(() => aGuid, () => emptyGuid).NotEmpty())
                .Message.Should().Contain("emptyGuid");
        }

        [Test]
        public void NotEmptyThrowsArgumentExceptionForEmptyGuidNew()
        {
            InspectionTestHelper.BatchTestInspection<GuidIsEmptyContractViolationException, Guid>(
                assert: inspected => inspected.NotEmpty(),
                badValues: new[] {Guid.Empty, new Guid()},
                goodValues: new[] {Guid.NewGuid(), Guid.NewGuid()});
        }
    }
}
