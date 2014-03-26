using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ManualInspectionTests
    {
        [Test]
        public void ThrownContractExceptionIfTestDoesNotPass()
        {
            var nameArgument = "bad";

            Assert.Throws<ContractViolationException>(() => Contract.Arguments(() => nameArgument).Inspect(value => value != nameArgument));
        }

        [Test]
        public void ThrowsExceptionMatchingParameterNameIfTestDoesNotPass()
        {
            var nameargument = "bad";

            Assert.Throws<ContractViolationException>(() => Contract.Arguments(() => nameargument).Inspect(value => value != nameargument))
                .Message.Should().Contain("nameargument");
        }
    }
}
