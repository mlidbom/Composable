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

        public async Task<(AccountResource.Command.Register.RegistrationAttemptResult Result, AccountResource Account)> ExecuteAsync()
        {
            var result = await _bus.Get(AccountApi.Start)
                       .Post(start => start.Commands.Register.Mutate(@this =>
                       {
                           @this.AccountId = AccountId;
                           @this.Email = Email;
                           @this.Password = Password;
                       }))
                       .ExecuteAsync();

            switch(result)
            {
                case AccountResource.Command.Register.RegistrationAttemptResult.Successful:
                    return (result, await _bus.Get(AccountApi.Start).Get(start => start.Queries.AccountById.WithId(AccountId)).ExecuteAsync());
                case AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    return (result, null);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
