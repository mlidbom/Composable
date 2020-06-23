using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class ManualInspectionTests
    {
        [Test]
        public void ThrownContractExceptionIfTestDoesNotPass()
        {
            var nameArgument = "bad";

            Assert.Throws<ContractViolationException>(() => Contract.Argument(nameArgument, nameof(nameArgument)).Inspect(value => value != nameArgument));
        }

        [Test]
        public void ThrowsExceptionMatchingParameterNameIfTestDoesNotPass()
        {
            var nameargument = "bad";

            Assert.Throws<ContractViolationException>(() => Contract.Argument(nameargument, nameof(nameargument)).Inspect(value => value != nameargument))
                .Message.Should().Contain("nameargument");
        }
    }
}
