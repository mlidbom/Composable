using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangePasswordScenario : ScenarioBase
    {
        readonly IEndpoint _clientEndpoint;

        public string OldPassword;
        public string NewPasswordAsString;
        public AccountResource Account { get; private set; }

        public static ChangePasswordScenario Create(IEndpoint domainEndpoint)
        {
            var registerAccountScenario = new RegisterAccountScenario(domainEndpoint);
            var account = registerAccountScenario.Execute().Account;

            return new ChangePasswordScenario(domainEndpoint, account, registerAccountScenario.Password);
        }

        public ChangePasswordScenario(IEndpoint clientEndpoint, AccountResource account, string oldPassword, string newPassword = null)
        {
            _clientEndpoint = clientEndpoint;
            Account = account;
            OldPassword = oldPassword;
            NewPasswordAsString = newPassword ?? TestData.Password.CreateValidPasswordString();
        }

        public void Execute()
        {
            Account.Commands.ChangePassword.WithValues(OldPassword, NewPasswordAsString).Post().ExecuteAsRequestOn(_clientEndpoint);

            Account = Api.Query.AccountById(Account.Id).ExecuteAsRequestOn(_clientEndpoint);
        }
    }
}
