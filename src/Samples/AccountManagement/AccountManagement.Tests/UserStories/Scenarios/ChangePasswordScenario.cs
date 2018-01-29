using AccountManagement.API;
using Composable.Contracts;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Linq;

namespace AccountManagement.UserStories.Scenarios
{
    class ChangePasswordScenario : ScenarioBase<AccountResource>
    {
        readonly IEndpoint _clientEndpoint;

        public string OldPassword;
        public string NewPassword;
        public AccountResource Account { get; private set; }

        public ChangePasswordScenario WithNewPassword(string newPassword) => this.Mutate(@this => @this.NewPassword = newPassword);
        public ChangePasswordScenario WithOldPassword(string oldPassword) => this.Mutate(@this => @this.OldPassword = oldPassword);

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
            NewPassword = newPassword;
        }

        public override AccountResource Execute()
        {
            Account.Commands.ChangePassword.WithValues(OldPassword, NewPassword).Post().ExecuteAsRequestOn(_clientEndpoint);

            return Account = Api.Query.AccountById(Account.Id).ExecuteAsRequestOn(_clientEndpoint);
        }
    }
}
