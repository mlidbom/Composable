using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    public class ChangeAccountEmailScenario
    {
        readonly IServiceBus _clientBus;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public AccountResource Account { get; private set; }

        //Review:mlidbo: Replace optional parameters in scenario constructors with constructor overloading throughout the sample project.
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
