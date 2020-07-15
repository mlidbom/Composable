using System;
using AccountManagement.API;
using AccountManagement.Domain.Registration;
using Composable.Messaging.Buses;
using Composable.System.Linq;

namespace AccountManagement.UserStories.Scenarios
{
    class RegisterAccountScenario : ScenarioBase<(AccountResource.Command.Register.RegistrationAttemptResult Result, AccountResource Account)>
    {
        readonly IEndpoint _clientEndpoint;

        public Guid AccountId;
        public String Email;
        public string Password;


        public RegisterAccountScenario WithAccountId(Guid acountId) => this.Mutate(@this => @this.AccountId = acountId);
        public RegisterAccountScenario WithEmail(string email) => this.Mutate(@this => @this.Email = email);
        public RegisterAccountScenario WithPassword(string password) => this.Mutate(@this => @this.Password = password);

        public RegisterAccountScenario(IEndpoint clientEndpoint, string email = null, string password = TestData.Passwords.ValidPassword)
        {
            _clientEndpoint = clientEndpoint;
            AccountId = Guid.NewGuid();
            Password = password;
            Email = email ?? TestData.Emails.CreateUnusedEmail();
        }

        public override (AccountResource.Command.Register.RegistrationAttemptResult Result, AccountResource Account) Execute()
        {
            var registrationAttemptResult = _clientEndpoint.ExecuteClientRequest(Api.Command.Register(AccountId, Email, Password));

            return registrationAttemptResult.Status switch
            {
                RegistrationAttemptStatus.Successful => (registrationAttemptResult, Api.Query.AccountById(AccountId).ExecuteAsClientRequestOn(_clientEndpoint)),
                RegistrationAttemptStatus.EmailAlreadyRegistered => (registrationAttemptResult, null),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
