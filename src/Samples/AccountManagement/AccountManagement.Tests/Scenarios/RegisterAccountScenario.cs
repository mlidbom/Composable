using System;
using AccountManagement.API;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class RegisterAccountScenario
    {
        readonly IServiceBus _bus;

        public AccountResource.RegisterAccountCommand Command { get; }

        public RegisterAccountScenario(IServiceBus bus, string email = null, string password = null)
        {
            _bus = bus;
            Command = AccountApi.Start.Commands.CreateAccount(accountId: Guid.NewGuid(),
                                                              password: password ?? TestData.Password.CreateValidPasswordString(),
                                                              email: email ?? TestData.Email.CreateValidEmail().ToString());
        }

        public AccountResource Execute() => _bus.SendAsync(Command).Result;
    }
}
