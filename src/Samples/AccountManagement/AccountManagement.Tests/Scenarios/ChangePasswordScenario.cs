using System;
using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangePasswordScenario
    {
        readonly IServiceBus _bus;

        public string OldPassword;
        public string NewPasswordAsString;
        public AccountResource Account { get; private set; }

        public static ChangePasswordScenario Create(IServiceBus bus)
        {
            var registerAccountScenario = new RegisterAccountScenario(bus);
            var account = registerAccountScenario.Execute().Account;

            return new ChangePasswordScenario(bus, account, registerAccountScenario.Password);
        }

        public ChangePasswordScenario(IServiceBus bus, AccountResource account, string oldPassword, string newPassword = null)
        {
            _bus = bus;
            Account = account;
            OldPassword = oldPassword;
            NewPasswordAsString = newPassword ?? TestData.Password.CreateValidPasswordString();
        }

        public void Execute()
        {
            var command = Account.CommandsCollections.ChangePassword;
            command.NewPassword = NewPasswordAsString;
            command.OldPassword = OldPassword;

            _bus.Send(command);
            Account = _bus.Execute(NavigationSpecification.Get(AccountApi.Start).Get(start => start.Queries.AccountById.WithId(Account.Id)));
        }
    }
}
