using AccountManagement.API;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Linq;

namespace AccountManagement.Scenarios
{
    class ChangePasswordScenario : ScenarioBase
    {
        readonly IEndpoint _clientEndpoint;

        public string OldPassword;
        public string NewPasswordAsString;
        public AccountResource Account { get; private set; }

        public ChangePasswordScenario SetNewPassword(string newPassword) => this.Mutate(@this => @this.NewPasswordAsString = newPassword);
        public ChangePasswordScenario SetOldPassword(string oldPassword) => this.Mutate(@this => @this.OldPassword = oldPassword);

        public static ChangePasswordScenario Create(IEndpoint domainEndpoint)
        {
            var registerAccountScenario = new RegisterAccountScenario(domainEndpoint);
            var account = registerAccountScenario.Execute().Account;

            return new ChangePasswordScenario(domainEndpoint, account, registerAccountScenario.Password, TestData.Passwords.CreateValidPasswordString());
        }

        public ChangePasswordScenario(IEndpoint clientEndpoint, AccountResource account, string oldPassword, string newPassword)
        {
            Assert.Argument.NotNull(account);
            _clientEndpoint = clientEndpoint;
            Account = account;
            OldPassword = oldPassword;
            NewPasswordAsString = newPassword;
        }

        public void Execute()
        {
            Account.Commands.ChangePassword.WithValues(OldPassword, NewPasswordAsString).PostRemote().ExecuteAsRequestOn(_clientEndpoint);

            Account = Api.Query.AccountById(Account.Id).ExecuteAsRequestOn(_clientEndpoint);
        }
    }
}
