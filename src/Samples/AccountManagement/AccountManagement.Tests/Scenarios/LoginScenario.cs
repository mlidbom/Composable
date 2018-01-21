using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Linq;

namespace AccountManagement.Tests.Scenarios
{
    class LoginScenario
    {
        readonly IEndpoint _domainEndpoint;
        public string Password { get; set; }
        public string Email { get; set; }

        public static LoginScenario Create(IEndpoint domainEndpoint)
        {
            var registerAccountScenario = new RegisterAccountScenario(domainEndpoint);
            registerAccountScenario.Execute();
            return new LoginScenario(domainEndpoint, registerAccountScenario.Email, registerAccountScenario.Password);
        }

        public LoginScenario(IEndpoint domainEndpoint, AccountResource account, string password) : this(domainEndpoint, account.Email.ToString(), password) {}

        public LoginScenario(IEndpoint domainEndpoint, string email, string password)
        {
            Email = email;
            Password = password;
            _domainEndpoint = domainEndpoint;
        }

        public AccountResource.Command.LogIn.LoginAttemptResult Execute()
        {
            return _domainEndpoint.ExecuteRequest(session => session.Execute(NavigationSpecification.GetRemote(AccountApi.Start)
                                          .PostRemote(start => start.Commands.Login.Mutate(@this =>
                                          {
                                              @this.Email = Email;
                                              @this.Password = Password;
                                          }))));
        }
    }
}
