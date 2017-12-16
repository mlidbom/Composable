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

        public ChangeAccountEmailScenario(IServiceBus clientBus, AccountResource account = null)
        {
            _clientBus = clientBus;
            Account = account ?? new RegisterAccountScenario(clientBus).Execute();
            OldEmail = Account.Email;
        }

        public void Execute()
        {
            _clientBus.Send(Account.Commands.ChangeEmail(NewEmail.ToString()));
            Account = _clientBus.Query(AccountApi.Start.Queries.AccountById(Account.Id));
        }
    }
}
