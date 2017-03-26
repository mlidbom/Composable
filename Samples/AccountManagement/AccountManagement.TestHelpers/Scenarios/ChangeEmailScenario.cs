using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Windsor;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class ChangeAccountEmailScenario
    {
        readonly IServiceLocator _container;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public readonly Account Account;

        //Review:mlidbo: Replace optional parameters in scenario constructors with constructor overloading throughout the sample project.
        public ChangeAccountEmailScenario(IServiceLocator container, Account account = null)
        {
            _container = container;
            Account = account ?? new RegisterAccountScenario(container).Execute();
            OldEmail = Account.Email;
        }

        public void Execute()
        {
            _container.ExecuteUnitOfWork(() => _container.Resolve<IAccountRepository>().Get(Account.Id).ChangeEmail(NewEmail));
        }
    }
}
