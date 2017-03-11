using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.AccountMapQueryModelTests
{
    public class RegistersAccountDuringSetupTestBase : QueryModelsTestsBase
    {
        protected Account RegisteredAccount;
        RegisterAccountScenario RegisterAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            RegisterAccountScenario = new RegisterAccountScenario(Container);
            RegisteredAccount = RegisterAccountScenario.Execute();
        }

        protected AccountQueryModel GetAccountQueryModel()
        {
            return QueryModelsReader.GetAccount(RegisteredAccount.Id);
        }
    }
}
