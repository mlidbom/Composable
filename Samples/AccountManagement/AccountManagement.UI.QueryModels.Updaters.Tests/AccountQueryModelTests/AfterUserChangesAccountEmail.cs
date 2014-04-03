using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDB.Readers.Services;
using Castle.MicroKernel.Lifestyle;
using Composable.CQRS.Windsor;
using Composable.KeyValueStorage;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.Tests.AccountQueryModelTests
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
                var reader = Container.Resolve<IAccountManagementDocumentDbReader>();
                var emailToAccountMap = reader.Get<EmailToAccountMapQueryModel>(_newEmail);

                emailToAccountMap.AccountId.Should().Be(RegisteredAccount.Id);
            }
        }

        [Test]
        public void TryingToFetchViaTheOldEmailThrowsNoSuchDocumentException()
        {
            using (Container.BeginScope())
            {
                Container.Resolve<IAccountManagementDocumentDbReader>()
                    .Invoking( me => me.Get<EmailToAccountMapQueryModel>(_oldEmail))
                    .ShouldThrow<NoSuchDocumentException>();

            }
        }
    }
}
