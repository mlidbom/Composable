using AccountManagement.API;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangePasswordScenario
    {
        readonly IServiceBus _clientBus;

        public string OldPassword;
        public string NewPasswordAsString = TestData.Password.CreateValidPasswordString();
        public AccountResource Account { get; private set; }

        public ChangePasswordScenario(IServiceBus clientBus)
        {
            _clientBus = clientBus;
            var registerAccountScenario = new RegisterAccountScenario(clientBus);
            Account = registerAccountScenario.Execute();
            OldPassword = registerAccountScenario.Command.Password;
        }

        public void Execute()
        {
            _clientBus.Send(Account.CommandsCollections.ChangePassword(oldPassword:OldPassword, newPassword: NewPasswordAsString));
            Account = _clientBus.Query(AccountApi.Start.Queries.AccountById(Account.Id));
        }
    }
}
