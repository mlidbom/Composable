using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDb;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Lifestyle;
using Composable.CQRS.Windsor;
using Composable.KeyValueStorage;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.EmailToAccountMapQueryModelTests
{
    [TestFixture]
    public class AfterUserChangesAccountEmail : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        private readonly Email _newEmail = Email.Parse("valid.email@domain.com");
        private Email _oldEmail;

        [SetUp]
        public void RegisterAccountAndChangeEmail()
        {
            _oldEmail = RegisteredAccount.Email;
            Container.ExecuteUnitOfWork(() => RegisteredAccount.ChangeEmail(_newEmail));
        }

        [Test]
        public void YouCanGetTheAccountViaTheNewEmail()
        {
            using(Container.BeginScope())
            {
                AccountQueryModel account;
                Container.Resolve<IAccountManagementQueryModelsReader>()
                    .TryGetAccountByEmail(_newEmail, out account)
                    .Should().Be(true);

                account.Id.Should().Be(RegisteredAccount.Id);
            }
        }

        [Test]
        public void TryingToFetchViaTheOldEmailThrowsNoSuchDocumentException()
        {
            using (Container.BeginScope())
            {
                AccountQueryModel account;
                Container.Resolve<IAccountManagementQueryModelsReader>()
                    .TryGetAccountByEmail(_oldEmail, out account)
                    .Should().Be(false);

                account.Should().Be(null);

            }
        }
    }
}
