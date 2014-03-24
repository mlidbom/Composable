using System;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.Contracts;
using Composable.KeyValueStorage.Population;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountFailureScenariosTests
    {
        private IDisposable _scope;
        private IAccountManagementEventStoreSession _repository;
        private readonly Password _validPassword = new Password("Password1");
        private readonly Email _validEmail = Email.Parse("test.test@test.se");
        private readonly Guid _validAccountId = Guid.NewGuid();
        private WindsorContainer _container;
        private IDuplicateAccountChecker _duplicateAccountChecker;

        [SetUp]
        public void SetupWiringAndCreateRepositoryAndScope()
        {
            _container = DomainTestWiringHelper.SetupContainerForTesting();
            _scope = _container.BeginScope();
            _repository = _container.Resolve<IAccountManagementEventStoreSession>();
            _duplicateAccountChecker = _container.Resolve<IDuplicateAccountChecker>();
        }

        [TearDown]
        public void CleanupScope()
        {
            _scope.Dispose();
        }

        [Test]
        public void WhenEmailIsAlreadyRegisteredADuplicateAccountExceptionIsThrown()
        {
            using(var transaction = _container.BeginTransactionalUnitOfWorkScope())
            {
                Account.Register(_validEmail, _validPassword, _validAccountId, _repository, _duplicateAccountChecker);
                transaction.Commit();
            }
            Assert.Throws<DuplicateAccountException>(() => Account.Register(_validEmail, _validPassword, _validAccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenPasswordIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(_validEmail, null, _validAccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenEmailIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsNullException>(() => Account.Register(null, _validPassword, _validAccountId, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenAccountIdIsEmptyObjectIsDefaultExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_validEmail, _validPassword, Guid.Empty, _repository, _duplicateAccountChecker));
        }

        [Test]
        public void WhenRepositoryIsNullObjectIsNullExceptionIsThrown()
        {
            Assert.Throws<ObjectIsDefaultException>(() => Account.Register(_validEmail, _validPassword, Guid.Empty, _repository, _duplicateAccountChecker));
        }
    }
}
