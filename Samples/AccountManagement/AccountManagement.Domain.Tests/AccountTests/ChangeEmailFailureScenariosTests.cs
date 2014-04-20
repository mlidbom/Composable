using System;
using AccountManagement.TestHelpers.Fixtures;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    public class ChangeEmailFailureScenariosTests : DomainTestBase
    {
        private Account _account;

        [SetUp]
        public void RegisterAccount()
        {
            _account = SingleAccountFixture.Setup(Container).Account;
        }

        [Test]
        public void WhenEmailIsNullObjectIsNullExceptionIsThrown()
        {
            _account.Invoking(account => account.ChangeEmail(null))
                .ShouldThrow<Exception>();
        }
    }
}
