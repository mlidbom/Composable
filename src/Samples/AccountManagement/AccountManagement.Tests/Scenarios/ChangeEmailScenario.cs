using AccountManagement.API;
using AccountManagement.Domain;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangeAccountEmailScenario
    {
        readonly IServiceBus _clientBus;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public AccountResource Account { get; private set; }

        public static ChangeAccountEmailScenario Create(IServiceBus clientBus)
            => new ChangeAccountEmailScenario(clientBus, new RegisterAccountScenario(clientBus).Execute().Account);

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

            _clientBus.PostRemote(command);

            Account = _clientBus.Execute(NavigationSpecification
                      .GetRemote(AccountApi.Start)
                      .GetRemote(start => start.Queries.AccountById.WithId(Account.Id)));

        }
    }
}
