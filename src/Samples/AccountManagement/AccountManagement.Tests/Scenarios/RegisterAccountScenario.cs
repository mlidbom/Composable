using System;
using AccountManagement.API;
using AccountManagement.Domain.Registration;
using Composable.Messaging.Buses;

namespace AccountManagement.Scenarios
{
    class RegisterAccountScenario : ScenarioBase
    {
        readonly IEndpoint _clientEndpoint;

        public Guid AccountId;
        public String Email;
        public string Password;

        public RegisterAccountScenario(IEndpoint clientEndpoint, string email = null, string password = null)
        {
            _clientEndpoint = clientEndpoint;
            AccountId = Guid.NewGuid();
            Password = password ?? TestData.Password.CreateValidPasswordString();
            Email = email ?? TestData.Email.CreateValidEmail().ToString();
        }

        public (AccountResource.Command.Register.RegistrationAttemptResult Result, AccountResource Account) Execute()
        {
            var registrationAttemptResult = _clientEndpoint.ExecuteRequest(Api.Command.Register(AccountId, Email, Password));

            switch(registrationAttemptResult.Status)
            {
                case RegistrationAttemptStatus.Successful:
                    return (registrationAttemptResult, Api.Query.AccountById(AccountId).ExecuteAsRequestOn(_clientEndpoint));
                case RegistrationAttemptStatus.EmailAlreadyRegistered:
                    return (registrationAttemptResult, null);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
