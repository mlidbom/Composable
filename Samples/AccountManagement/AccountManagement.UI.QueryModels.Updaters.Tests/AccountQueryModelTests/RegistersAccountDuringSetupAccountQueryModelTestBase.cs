using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Updaters.Tests.AccountQueryModelTests
{
    public class RegistersAccountDuringSetupAccountQueryModelTestBase : QueryModelsUpdatersTestsBase
    {
        protected Account RegisteredAccount;
        protected RegisterAccountScenario RegisterAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            RegisterAccountScenario = new RegisterAccountScenario(Container);
            RegisteredAccount = RegisterAccountScenario.Execute();
        }

        protected AccountQueryModel GetQueryModel()
        {
            return Session.Get<AccountQueryModel>(RegisteredAccount.Id);
        }
    }
}
