using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.FetchingAccountByEmailTests
{
    public class RegistersAccountDuringSetupAccountQueryModelTestBase : QueryModelsTestsBase
    {
        protected Account RegisteredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(Container);
            RegisteredAccount = _registerAccountScenario.Execute();
        }
    }
}
