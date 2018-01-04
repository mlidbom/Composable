using System;
using System.Threading.Tasks;
using AccountManagement.API;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.System.Threading;

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
            var result = _bus.Get(AccountApi.Start)
                       .Post(start => start.Commands.Register.Mutate(@this =>
                       {
                           @this.AccountId = AccountId;
                           @this.Email = Email;
                           @this.Password = Password;
                       }))
                       .Execute();

            switch(result)
            {
                case AccountResource.Command.Register.RegistrationAttemptResult.Successful:
                    return (result, _bus.Get(AccountApi.Start).Get(start => start.Queries.AccountById.WithId(AccountId)).Execute());
                case AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    return (result, null);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
