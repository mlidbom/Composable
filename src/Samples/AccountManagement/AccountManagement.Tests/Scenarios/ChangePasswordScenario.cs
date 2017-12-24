using AccountManagement.API;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangePasswordScenario
    {
        readonly IServiceBus _bus;

        public string OldPassword;
        public string NewPasswordAsString = TestData.Password.CreateValidPasswordString();
        public AccountResource Account { get; private set; }

        public ChangePasswordScenario(IServiceBus bus)
        {
            _bus = bus;
            var registerAccountScenario = new RegisterAccountScenario(bus);
            Account = registerAccountScenario.Execute();
            OldPassword = registerAccountScenario.Password;
        }

        public void Execute()
        {
            var command = Account.CommandsCollections.ChangePassword;
            command.NewPassword = NewPasswordAsString;
            command.OldPassword = OldPassword;

            _bus.Send(command);
            Account = _bus.Query(AccountApi.Start.Get().Queries.AccountById.WithId(Account.Id));
        }
    }
}
