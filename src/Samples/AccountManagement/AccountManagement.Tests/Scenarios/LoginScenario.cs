using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Linq;

namespace AccountManagement.Tests.Scenarios
{
    class LoginScenario
    {
        readonly IServiceBusSession _busSession;
        public string Password { get; set; }
        public string Email { get; set; }

        public static LoginScenario Create(IServiceBusSession busSession)
        {
            var registerAccountScenario = new RegisterAccountScenario(busSession);
            registerAccountScenario.Execute();
            return new LoginScenario(busSession, registerAccountScenario.Email, registerAccountScenario.Password);
        }

        public LoginScenario(IServiceBusSession busSession, AccountResource account, string password) : this(busSession, account.Email.ToString(), password) {}

        public LoginScenario(IServiceBusSession busSession, string email, string password)
        {
            Email = email;
            Password = password;
            _busSession = busSession;
        }

        public AccountResource.Command.LogIn.LoginAttemptResult Execute()
        {
            return _busSession.Execute(NavigationSpecification.GetRemote(AccountApi.Start)
                                          .PostRemote(start => start.Commands.Login.Mutate(@this =>
                                          {
                                              @this.Email = Email;
                                              @this.Password = Password;
                                          })));
        }
    }
}
