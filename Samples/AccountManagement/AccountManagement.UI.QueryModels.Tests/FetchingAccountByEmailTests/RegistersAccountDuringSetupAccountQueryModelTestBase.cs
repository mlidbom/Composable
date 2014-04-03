using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.EmailToAccountMapQueryModelTests
{
    public class RegistersAccountDuringSetupAccountQueryModelTestBase : QueryModelsTestsBase
    {
        protected Account RegisteredAccount;
        protected RegisterAccountScenario RegisterAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            RegisterAccountScenario = new RegisterAccountScenario(Container);
            RegisteredAccount = RegisterAccountScenario.Execute();
        }
    }
}
