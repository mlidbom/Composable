using System.Threading.Tasks;
using AccountManagement.API;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class LoginScenario
    {
        readonly IServiceBus _bus;
        public string Password { get; set; }
        public string Email { get; set; }

        public LoginScenario(IServiceBus bus)
        {
            _bus = bus;
            var registerAccountScenario = new RegisterAccountScenario(bus);
            Email = registerAccountScenario.Execute().Email.ToString();
            Password = registerAccountScenario.Password;
        }

        public LoginScenario(IServiceBus bus, AccountResource account, string password):this(bus, account.Email.ToString(), password)
        {}

        public LoginScenario(IServiceBus bus, string email, string password)
        {
            Email = email;
            Password = password;
            _bus = bus;
        }

        public Task<LoginAttemptResult> ExecuteAsync()
        {
            return _bus.Get(AccountApi.Start)
                .Post(start => start.Commands.Login.New(Email, Password))
                .ExecuteAsync();
        }
    }
}
