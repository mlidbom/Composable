using AccountManagement.Domain;
using AccountManagement.Tests.Scenarios;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.QueryModels.FetchingAccountByEmailTests
{
    public class RegistersAccountDuringSetupAccountQueryModelTestBase : QueryModelsTestsBase
    {
        protected Account RegisteredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ServiceLocator);
            RegisteredAccount = _registerAccountScenario.Execute();
        }
    }
}
