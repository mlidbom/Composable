using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Composable.DependencyInjection;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class RegisterAccountScenario
    {
        readonly IServiceLocator _serviceLocator;
        public readonly string PasswordAsString = TestData.Password.CreateValidPasswordString();
        public Password Password;
        public Email Email = TestData.Email.CreateValidEmail();
        public Guid AccountId = Guid.NewGuid();

        public RegisterAccountScenario(IServiceLocator serviceLocator)
        {
            Password = new Password(PasswordAsString);
            _serviceLocator = serviceLocator;
        }

        public Account Execute()
        {
            return _serviceLocator.ExecuteUnitOfWork(
                () =>
                {
                    using(var duplicateAccountChecker = _serviceLocator.Lease<IDuplicateAccountChecker>())
                    using(var repository = _serviceLocator.Lease<IAccountRepository>())
                    {
                        var registered = Account.Register(Email, Password, AccountId, repository.Instance, duplicateAccountChecker.Instance);

                        return registered;
                    }
                });

        }
    }
}
