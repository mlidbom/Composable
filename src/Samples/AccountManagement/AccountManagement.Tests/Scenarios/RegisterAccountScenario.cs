using System;
using AccountManagement.API;
using AccountManagement.API.UserCommands;
using AccountManagement.Domain;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    public class RegisterAccountScenario
    {
        readonly IServiceBus _bus;

        public RegisterAccountCommand Command { get; }

        public RegisterAccountScenario(IServiceBus bus, string email = null, string password = null)
        {
            _bus = bus;
            Command = new RegisterAccountCommand()
                      {
                          Password = password ?? TestData.Password.CreateValidPasswordString(),
                          Email = email ?? TestData.Email.CreateValidEmail().ToString(),
                          AccountId = Guid.NewGuid()
                      };
        }

        public AccountResource Execute() => _bus.SendAsync(Command).Result;
    }
}
