using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    public class ChangeAccountEmailScenario
    {
        readonly IServiceLocator _serviceLocator;
        readonly IServiceBus _clientBus;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public AccountResource Account { get; private set; }

        //Review:mlidbo: Replace optional parameters in scenario constructors with constructor overloading throughout the sample project.
        public ChangeAccountEmailScenario(IServiceLocator serviceLocator, IServiceBus clientBus, AccountResource account = null)
        {
            _serviceLocator = serviceLocator;
            _clientBus = clientBus;
            Account = account ?? new RegisterAccountScenario(clientBus).Execute();
            OldEmail = Account.Email;
        }

        public void Execute()
        {
            _serviceLocator.ExecuteTransaction(() => _serviceLocator.Use<IAccountRepository>(repo => repo.Get(Account.Id).ChangeEmail(NewEmail)));

            Account = _clientBus.Query(AccountApi.Start.Queries.AccountById(Account.Id));
        }
    }
}
