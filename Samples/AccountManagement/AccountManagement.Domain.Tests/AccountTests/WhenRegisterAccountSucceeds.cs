using System;
using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Castle.MicroKernel.Lifestyle;
using Composable.Contracts;
using Composable.KeyValueStorage.Population;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountSuccess
    {
        private IAccountManagementEventStoreSession _repository;
        private const string ValidPasswordString = "Password1";
        private readonly Password _password = new Password(ValidPasswordString);
        private readonly Email _validEmail = Email.Parse("test.test@test.se");
        private readonly Guid _validAccountId = Guid.NewGuid();
        private MessageSpy _messageSpy;
        private Account _registered;

        [SetUp]
        public void ExecuteSuccessScenario()
        {
            var container = DomainTestWiringHelper.SetupContainerForTesting();
            using(container.BeginScope())
            {
                using(var transaction = container.BeginTransactionalUnitOfWorkScope())
                {
                    _messageSpy = container.Resolve<MessageSpy>();
                    _repository = container.Resolve<IAccountManagementEventStoreSession>();
                    _registered = Account.Register(_validEmail, _password, _validAccountId, _repository);
                    transaction.Commit();
                }
            }
        }

        [Test]
        public void AnIUserRegisteredAccountEventIsPublished()
        {
            _messageSpy.ReceivedMessages.OfType<IUserRegisteredAccountEvent>().ToList().Should().HaveCount(1);
        }
    }
}
