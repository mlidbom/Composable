using System;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Composable.Tests.Contracts
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

            Assert.Throws<GuidIsEmptyContractViolationException>(() => Contract.Argument(emptyGuid, nameof(emptyGuid)).NotEmpty());
            Assert.Throws<GuidIsEmptyContractViolationException>(() => Contract.Argument(emptyGuid, nameof(emptyGuid)).NotEmpty())
                .Message.Should().Contain("emptyGuid");

            Assert.Throws<GuidIsEmptyContractViolationException>(() => Contract.Argument(aGuid, nameof(aGuid),emptyGuid, nameof(emptyGuid)).NotEmpty())
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
