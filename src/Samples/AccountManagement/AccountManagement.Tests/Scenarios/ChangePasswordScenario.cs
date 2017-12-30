using System.Threading.Tasks;
using AccountManagement.API;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangePasswordScenario
    {
        readonly IServiceBus _bus;

        public string OldPassword;
        public string NewPasswordAsString;
        public AccountResource Account { get; private set; }

        public static async Task<ChangePasswordScenario> CreateAsync(IServiceBus bus)
        {
            var registerAccountScenario = new RegisterAccountScenario(bus);
            var account = (await registerAccountScenario.ExecuteAsync()).Account;

            return new ChangePasswordScenario(bus, account, registerAccountScenario.Password);
        }

        public ChangePasswordScenario(IServiceBus bus, AccountResource account, string oldPassword, string newPassword = null)
        {
            _bus = bus;
            Account = account;
            OldPassword = oldPassword;
            NewPasswordAsString = newPassword ?? TestData.Password.CreateValidPasswordString();
        }

        public async Task ExecuteAsync()
        {
            var command = Account.CommandsCollections.ChangePassword;
            command.NewPassword = NewPasswordAsString;
            command.OldPassword = OldPassword;

            await _bus.SendAsync(command);
            Account = await _bus.Get(AccountApi.Start).Get(start => start.Queries.AccountById.WithId(Account.Id)).ExecuteAsync();
        }
    }
}
