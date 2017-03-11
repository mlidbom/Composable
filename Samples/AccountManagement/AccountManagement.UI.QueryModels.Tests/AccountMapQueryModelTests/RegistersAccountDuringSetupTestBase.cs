using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.AccountMapQueryModelTests
{
    public class RegistersAccountDuringSetupTestBase : QueryModelsTestsBase
    {
        protected Account RegisteredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(Container);
            RegisteredAccount = _registerAccountScenario.Execute();
        }

        protected AccountQueryModel GetAccountQueryModel()
        {
            return QueryModelsReader.GetAccount(RegisteredAccount.Id);
        }
    }
}
