using System;
using AccountManagement.API;
using AccountManagement.API.UserCommands;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    public class RegisterAccountScenarioNew
    {
        readonly IServiceBus _bus;

        public RegisterAccountCommand Command { get; } = new RegisterAccountCommand()
                                                         {
                                                             Password = TestData.Password.CreateValidPasswordString(),
                                                             Email = TestData.Email.CreateValidEmail().ToString(),
                                                             AccountId = Guid.NewGuid()
                                                         };

        public RegisterAccountScenarioNew(IServiceBus bus) => _bus = bus;

        public AccountResource Execute() => _bus.SendAsync(Command).Result;
    }
}
