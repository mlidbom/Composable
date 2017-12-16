using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Tests.Scenarios;
using AccountManagement.UI.QueryModels;
using NUnit.Framework;

namespace AccountManagement.Tests.UI.QueryModels.AccountMapQueryModelTests
{
    public class RegistersAccountDuringSetupTestBase : QueryModelsTestsBase
    {
        protected AccountResource RegisteredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            RegisteredAccount = _registerAccountScenario.Execute();
        }

        protected AccountQueryModel GetAccountQueryModel() => QueryModelsReader.GetAccount(RegisteredAccount.Id);
    }
}
