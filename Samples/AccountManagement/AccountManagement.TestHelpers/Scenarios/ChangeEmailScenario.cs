using AccountManagement.Domain;
using AccountManagement.Domain.Shared;
using Castle.Windsor;
using Composable.CQRS.Windsor;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class ChangeAccountEmailScenario
    {
        private readonly IWindsorContainer _container;

        public Email NewEmail = TestData.Email.CreateValidEmail();
        public readonly Email OldEmail;
        public readonly Account Account;

        public ChangeAccountEmailScenario(IWindsorContainer container)
        {
            _container = container;
            var registerAccountScenario = new RegisterAccountScenario(container);
            Account = registerAccountScenario.Execute();
            OldEmail = Account.Email;
        }

        public void Execute()
        {            
            _container.ExecuteUnitOfWork(() => Account.ChangeEmail(NewEmail));
        }
    }
}
