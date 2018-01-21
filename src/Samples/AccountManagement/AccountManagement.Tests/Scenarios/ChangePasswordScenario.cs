using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangePasswordScenario
    {
        readonly IRemoteServiceBusSession _busSession;

        public string OldPassword;
        public string NewPasswordAsString;
        public AccountResource Account { get; private set; }

        public static ChangePasswordScenario Create(IRemoteServiceBusSession busSession)
        {
            var registerAccountScenario = new RegisterAccountScenario(busSession);
            var account = registerAccountScenario.Execute().Account;

            return new ChangePasswordScenario(busSession, account, registerAccountScenario.Password);
        }

        public ChangePasswordScenario(IRemoteServiceBusSession busSession, AccountResource account, string oldPassword, string newPassword = null)
        {
            _busSession = busSession;
            Account = account;
            OldPassword = oldPassword;
            NewPasswordAsString = newPassword ?? TestData.Password.CreateValidPasswordString();
        }

        public void Execute()
        {
            var command = Account.CommandsCollections.ChangePassword;
            command.NewPassword = NewPasswordAsString;
            command.OldPassword = OldPassword;

            _busSession.PostRemote(command);
            Account = _busSession.Execute(NavigationSpecification
                      .GetRemote(AccountApi.Start)
                      .GetRemote(start => start.Queries.AccountById.WithId(Account.Id)));
        }
    }
}
