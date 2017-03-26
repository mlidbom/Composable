using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Composable.DependencyInjection;
using Composable.Windsor;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class RegisterAccountScenario
    {
        readonly IServiceLocator _container;
        public string PasswordAsString = TestData.Password.CreateValidPasswordString();
        public Password Password;
        public Email Email = TestData.Email.CreateValidEmail();
        public Guid AccountId = Guid.NewGuid();

        public RegisterAccountScenario(IServiceLocator container)
        {
            Password = new Password(PasswordAsString);
            _container = container;
        }

        public Account Execute()
        {
            return _container.ExecuteUnitOfWork(
                () =>
                {
                    var repository = _container.Resolve<IAccountRepository>();
                    var duplicateAccountChecker = _container.Resolve<IDuplicateAccountChecker>();
                    var registered = Account.Register(Email, Password, AccountId, repository, duplicateAccountChecker);

                    return registered;
                });

        }
    }
}
