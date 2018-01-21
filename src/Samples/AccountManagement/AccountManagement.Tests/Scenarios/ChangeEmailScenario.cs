using AccountManagement.API;
using AccountManagement.Domain;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangeAccountEmailScenario
    {
        readonly IServiceBusSession _clientBusSession;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public AccountResource Account { get; private set; }

        public static ChangeAccountEmailScenario Create(IServiceBusSession clientBusSession)
            => new ChangeAccountEmailScenario(clientBusSession, new RegisterAccountScenario(clientBusSession).Execute().Account);

        public ChangeAccountEmailScenario(IServiceBusSession clientBusSession, AccountResource account)
        {
            _clientBusSession = clientBusSession;
            Account = account;
            OldEmail = Account.Email;
        }

        public void Execute()
        {
            var command = Account.CommandsCollections.ChangeEmail;
            command.Email = NewEmail.ToString();

            _clientBusSession.PostRemote(command);

            Account = _clientBusSession.Execute(NavigationSpecification
                      .GetRemote(AccountApi.Start)
                      .GetRemote(start => start.Queries.AccountById.WithId(Account.Id)));

        }
    }
}
