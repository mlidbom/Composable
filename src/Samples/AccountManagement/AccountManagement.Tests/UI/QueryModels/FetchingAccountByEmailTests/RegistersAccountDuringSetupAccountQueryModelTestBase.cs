using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Tests.Scenarios;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.QueryModels.FetchingAccountByEmailTests
{
    public class RegistersAccountDuringSetupAccountQueryModelTestBase : QueryModelsTestsBase
    {
        protected AccountResource RegisteredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            RegisteredAccount = _registerAccountScenario.Execute();
        }
    }
}
