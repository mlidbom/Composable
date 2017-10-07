using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class ManualInspectionTests
    {
        [Test]
        public void ThrownContractExceptionIfTestDoesNotPass()
        {
            var nameArgument = "bad";

            Assert.Throws<ContractViolationException>(() => OldContract.Argument(() => nameArgument).Inspect(value => value != nameArgument));
        }

        [Test]
        public void ThrowsExceptionMatchingParameterNameIfTestDoesNotPass()
        {
            var nameargument = "bad";

            Assert.Throws<ContractViolationException>(() => OldContract.Argument(() => nameargument).Inspect(value => value != nameargument))
                .Message.Should().Contain("nameargument");
        }
    }
}
