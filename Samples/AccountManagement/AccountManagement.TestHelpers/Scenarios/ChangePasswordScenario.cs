using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.Windsor;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class ChangePasswordScenario
    {
        readonly IWindsorContainer _container;

        public string OldPassword;
        public readonly string NewPasswordAsString = TestData.Password.CreateValidPasswordString();
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
            _container.ExecuteUnitOfWork(() => _container.Resolve<IAccountRepository>()
                                                         .Get(Account.Id)
                                                         .ChangePassword(oldPassword: OldPassword,
                                                                         newPassword: NewPassword));
        }
    }
}
