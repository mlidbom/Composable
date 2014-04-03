using AccountManagement.Domain.Shared;
using Castle.MicroKernel.Lifestyle;
using Composable.CQRS.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.Tests.AccountQueryModelTests
{
    [TestFixture]
    public class AfterUserChangesAccountPassword : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        private readonly Password _newPassword = new Password("ComplexPassword!1");

        [SetUp]
        public void ChangePassword()
        {
            Container.ExecuteUnitOfWork(() => RegisteredAccount.ChangePassword(RegisterAccountScenario.PasswordAsString, _newPassword));
        }

        [Test]
        public void PasswordIsTheNewOne()
        {
            using(Container.BeginScope())
            {
                GetQueryModel().Password.Should().Be(_newPassword);   
            }            
        }
    }
}
