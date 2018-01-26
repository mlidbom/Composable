using AccountManagement.API;
using AccountManagement.Domain;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Scenarios
{
    class ChangeAccountEmailScenario : ScenarioBase
    {
        readonly IEndpoint _clientEndpoint;

        public string NewEmail = TestData.Email.CreateValidEmail().ToString();
        public readonly Email OldEmail;
        public AccountResource Account { get; private set; }

        public static ChangeAccountEmailScenario Create(IEndpoint domainEndpoint)
            => new ChangeAccountEmailScenario(domainEndpoint, new RegisterAccountScenario(domainEndpoint).Execute().Account);

        public ChangeAccountEmailScenario(IEndpoint clientEndpoint, AccountResource account)
        {
            _clientEndpoint = clientEndpoint;
            Account = account;
            OldEmail = Account.Email;
        }

        public void Execute()
        {
            Account.Commands.ChangeEmail.WithEmail(NewEmail).PostRemote().ExecuteAsRequestOn(_clientEndpoint);

            Account = Api.Query.AccountById(Account.Id).ExecuteAsRequestOn(_clientEndpoint);
        }
    }
}
