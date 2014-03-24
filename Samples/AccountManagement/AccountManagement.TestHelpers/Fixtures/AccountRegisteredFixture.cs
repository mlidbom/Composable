using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;

namespace AccountManagement.TestHelpers.Fixtures
{
    public class AccountRegisteredFixture
    {
        public string PasswordAsString = "Password1";
        public readonly Password Password = new Password("Password1");
        public readonly Email Email = Email.Parse("test.test@test.se");
        public readonly Guid AccountId = Guid.NewGuid();        

        public Account Setup(IWindsorContainer container)
        {
            using (var transaction = container.BeginTransactionalUnitOfWorkScope())
            {
                var repository = container.Resolve<IAccountManagementEventStoreSession>();
                var duplicateAccountChecker = container.Resolve<IDuplicateAccountChecker>();
                var registered = Account.Register(Email, Password, AccountId, repository, duplicateAccountChecker);
                transaction.Commit();
                return registered;
            }
        }
    }
}