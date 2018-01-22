using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangePasswordScenario
    {
        readonly IEndpoint _domainEndpoint;

        public string OldPassword;
        public string NewPasswordAsString;
        public AccountResource Account { get; private set; }

        public static ChangePasswordScenario Create(IEndpoint domainEndpoint)
        {
            var registerAccountScenario = new RegisterAccountScenario(domainEndpoint);
            var account = registerAccountScenario.Execute().Account;

            return new ChangePasswordScenario(domainEndpoint, account, registerAccountScenario.Password);
        }

        public ChangePasswordScenario(IEndpoint domainEndpoint, AccountResource account, string oldPassword, string newPassword = null)
        {
            _domainEndpoint = domainEndpoint;
            Account = account;
            OldPassword = oldPassword;
            NewPasswordAsString = newPassword ?? TestData.Password.CreateValidPasswordString();
        }

        public void Execute()
        {
            var command = Account.CommandsCollections.ChangePassword;
            command.NewPassword = NewPasswordAsString;
            command.OldPassword = OldPassword;

            _domainEndpoint.ExecuteRequest(session => session.PostRemote(command));

            Account = _domainEndpoint.ExecuteRequest(AccountApi.Query.AccountById(Account.Id));
        }
    }
}
