using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;

namespace AccountManagement.TestHelpers.Scenarios
{
    public class RegisterAccountScenario
    {
        private readonly IWindsorContainer _container;
        public string PasswordAsString = "Password1";
        public Password Password = new Password("Password1");
        public Email Email = Email.Parse("test.test@test.se");
        public Guid AccountId = Guid.NewGuid();

        public RegisterAccountScenario(IWindsorContainer container)
        {
            _container = container;
        }

        public Account Execute()
        {
            using(var transaction = _container.BeginTransactionalUnitOfWorkScope())
            {
                var repository = _container.Resolve<IAccountRepository>();
                var duplicateAccountChecker = _container.Resolve<IDuplicateAccountChecker>();
                var registered = Account.Register(Email, Password, AccountId, repository, duplicateAccountChecker);
                transaction.Commit();
                return registered;
            }
        }
    }
}
