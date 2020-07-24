using AccountManagement.API;
using AccountManagement.Domain;
using Composable.Messaging.Buses;
using Composable.Messaging.Hypermedia;
using Composable.SystemCE.LinqCE;

namespace AccountManagement.UserStories.Scenarios
{
    class ChangeAccountEmailScenario : ScenarioBase<AccountResource>
    {
        readonly IEndpoint _clientEndpoint;

        public string NewEmail = TestData.Emails.CreateUnusedEmail();
        public readonly Email OldEmail;

        public ChangeAccountEmailScenario WithNewEmail(string newEmail) => this.Mutate(@this => @this.NewEmail = newEmail);


        public AccountResource Account { get; private set; }

        public static ChangeAccountEmailScenario Create(IEndpoint domainEndpoint)
            => new ChangeAccountEmailScenario(domainEndpoint, new RegisterAccountScenario(domainEndpoint).Execute().Account);

        public ChangeAccountEmailScenario(IEndpoint clientEndpoint, AccountResource account)
        {
            _clientEndpoint = clientEndpoint;
            Account = account;
            OldEmail = Account.Email;
        }

        public override AccountResource Execute()
        {
            Account.Commands.ChangeEmail.WithEmail(NewEmail).Post().ExecuteAsClientRequestOn(_clientEndpoint);

            return Account = Api.Query.AccountById(Account.Id).ExecuteAsClientRequestOn(_clientEndpoint);
        }
    }
}
