using System;
using AccountManagement.API;
using Composable.Messaging.Buses;
using Composable.System.Linq;

namespace AccountManagement.Tests.Scenarios
{
    class RegisterAccountScenario
    {
        readonly IServiceBus _bus;

        public Guid AccountId;
        public String Email;
        public string Password;

        public RegisterAccountScenario(IServiceBus bus, string email = null, string password = null)
        {
            _bus = bus;
            AccountId = Guid.NewGuid();
            Password = password ?? TestData.Password.CreateValidPasswordString();
            Email = email ?? TestData.Email.CreateValidEmail().ToString();
        }

        public AccountResource Execute()
        {
            return _bus.Get(AccountApi.Start)
                       .Post(start => start.Commands.Register.Mutate(@this =>
                       {
                           @this.AccountId = AccountId;
                           @this.Email = Email;
                           @this.Password = Password;
                       }))
                       .Execute();
        }
    }
}
