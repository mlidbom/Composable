using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.Domain;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangeAccountEmailScenario
    {
        readonly IServiceBus _clientBus;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public AccountResource Account { get; private set; }

        public static async Task<ChangeAccountEmailScenario> CreateAsync(IServiceBus clientBus)
            => new ChangeAccountEmailScenario(clientBus, await new RegisterAccountScenario(clientBus).ExecuteAsync());

        public ChangeAccountEmailScenario(IServiceBus clientBus, AccountResource account)
        {
            _clientBus = clientBus;
            Account = account;
            OldEmail = Account.Email;
        }

        public void Execute()
        {
            var command = Account.CommandsCollections.ChangeEmail;
            command.Email = NewEmail.ToString();

            _clientBus.Send(command);

            Account = _clientBus.Query(AccountApi.Start.Get().Queries.AccountById.WithId(Account.Id));
        }
    }
}
