using AccountManagement.API;
using Composable.Messaging.Buses;
using Composable.System.Linq;

namespace AccountManagement.UserStories.Scenarios
{
    class LoginScenario : ScenarioBase<AccountResource.Command.LogIn.LoginAttemptResult>
    {
        readonly IEndpoint _clientEndpoint;
        public string Password { get; set; }
        public string Email { get; set; }


        public LoginScenario WithEmail(string email) => this.Mutate(@this => @this.Email = email);
        public LoginScenario WithPassword(string password) => this.Mutate(@this => @this.Password = password);

        public static LoginScenario Create(IEndpoint clientEndpoint)
        {
            var registerAccountScenario = new RegisterAccountScenario(clientEndpoint);
            registerAccountScenario.Execute();
            return new LoginScenario(clientEndpoint, registerAccountScenario.Email, registerAccountScenario.Password);
        }

        public LoginScenario(IEndpoint clientEndpoint, AccountResource account, string password) : this(clientEndpoint, account.Email.ToString(), password) {}

        public LoginScenario(IEndpoint clientEndpoint, string email, string password)
        {
            Email = email;
            Password = password;
            _clientEndpoint = clientEndpoint;
        }

        public override AccountResource.Command.LogIn.LoginAttemptResult Execute() => Api.Command.Login(Email, Password).ExecuteAsRequestOn(_clientEndpoint);
    }
}
