using AccountManagement.API;
using AccountManagement.Domain;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class ChangeAccountEmailScenario
    {
        readonly IEndpoint _domainEndpoint;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public AccountResource Account { get; private set; }

        public static ChangeAccountEmailScenario Create(IEndpoint domainEndpoint)
            => new ChangeAccountEmailScenario(domainEndpoint, new RegisterAccountScenario(domainEndpoint).Execute().Account);

        public ChangeAccountEmailScenario(IEndpoint domainEndpoint, AccountResource account)
        {
            _domainEndpoint = domainEndpoint;
            Account = account;
            OldEmail = Account.Email;
        }

        public void Execute()
        {
            var command = Account.CommandsCollections.ChangeEmail;
            command.Email = NewEmail.ToString();

            _domainEndpoint.ExecuteRequest(session => session.PostRemote(command));

            Account = _domainEndpoint.ExecuteRequest(session => session.Execute(NavigationSpecification
                      .GetRemote(AccountApi.Start)
                      .GetRemote(start => start.Queries.AccountById.WithId(Account.Id))));

        }
    }
}
