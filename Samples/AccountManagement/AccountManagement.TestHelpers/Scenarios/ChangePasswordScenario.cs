using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Composable.DependencyInjection;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class ChangePasswordScenario
    {
        readonly IServiceLocator _serviceLocator;

        public string OldPassword;
        public readonly string NewPasswordAsString = TestData.Password.CreateValidPasswordString();
        public Password NewPassword;
        public readonly Account Account;

        public ChangePasswordScenario(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
            NewPassword = new Password(NewPasswordAsString);
            var registerAccountScenario = new RegisterAccountScenario(serviceLocator);
            Account = registerAccountScenario.Execute();
            OldPassword = registerAccountScenario.PasswordAsString;
        }

        public void Execute()
        {
            _serviceLocator.ExecuteUnitOfWork(() => _serviceLocator.Use<IAccountRepository>(repo => repo.Get(Account.Id)
                                                                                              .ChangePassword(oldPassword: OldPassword,
                                                                                                              newPassword: NewPassword)));
        }
    }
}
