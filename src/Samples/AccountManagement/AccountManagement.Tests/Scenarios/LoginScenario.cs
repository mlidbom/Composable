using AccountManagement.API;
using Composable.Messaging.Buses;

namespace AccountManagement.Scenarios
{
    class LoginScenario : ScenarioBase
    {
        readonly IEndpoint _clientEndpoint;
        public string Password { get; set; }
        public string Email { get; set; }

        public static LoginScenario Create(IEndpoint domainEndpoint)
        {
            var registerAccountScenario = new RegisterAccountScenario(domainEndpoint);
            registerAccountScenario.Execute();
            return new LoginScenario(domainEndpoint, registerAccountScenario.Email, registerAccountScenario.Password);
        }

        public LoginScenario(IEndpoint clientEndpoint, AccountResource account, string password) : this(clientEndpoint, account.Email.ToString(), password) {}

        public LoginScenario(IEndpoint clientEndpoint, string email, string password)
        {
            Email = email;
            Password = password;
            _clientEndpoint = clientEndpoint;
        }

        public AccountResource.Command.LogIn.LoginAttemptResult Execute() => Api.Command.Login(Email, Password).ExecuteAsRequestOn(_clientEndpoint);
    }
}
