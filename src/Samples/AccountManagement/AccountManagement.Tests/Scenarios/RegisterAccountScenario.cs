using System;
using AccountManagement.API;
using Composable.Messaging;
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

        public (AccountResource.Command.Register.RegistrationAttemptResult Result, AccountResource Account) Execute()
        {
            var result = _bus.Execute(NavigationSpecification.GetRemote(AccountApi.Start)
                                                .PostRemote(start => start.Commands.Register.Mutate(@this =>
                                                {
                                                    @this.AccountId = AccountId;
                                                    @this.Email = Email;
                                                    @this.Password = Password;
                                                })));

            switch(result)
            {
                case AccountResource.Command.Register.RegistrationAttemptResult.Successful:
                    return (result, _bus.Execute(NavigationSpecification.GetRemote(AccountApi.Start).GetRemote(start => start.Queries.AccountById.WithId(AccountId))));
                case AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    return (result, null);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
