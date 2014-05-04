using AccountManagement.Domain;
using AccountManagement.Domain.Shared;
using Castle.Windsor;
using Composable.CQRS.Windsor;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class ChangePasswordScenario
    {
        private readonly IWindsorContainer _container;

        public string OldPassword;
        public readonly string NewPasswordAsString = TestData.Password.CreateValidPassword();
        public Password NewPassword;
        public readonly Account Account;

        public ChangePasswordScenario(IWindsorContainer container)
        {
            _container = container;
            NewPassword = new Password(NewPasswordAsString);
            var registerAccountScenario = new RegisterAccountScenario(container);
            Account = registerAccountScenario.Execute();
            OldPassword = registerAccountScenario.PasswordAsString;
        }

        public void Execute()
        {            
            _container.ExecuteUnitOfWork(() => Account.ChangePassword(
                oldPassword: OldPassword, 
                newPassword: NewPassword));
        }
    }
}
