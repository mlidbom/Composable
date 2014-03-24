using System;
using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using Castle.MicroKernel.Lifestyle;
using Composable.KeyValueStorage.Population;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.AccountTests
{
    [TestFixture]
    public class RegisterAccountSuccessScenarioTests
    {
        private const string _registrationPasswordAsString = "Password1";
        private readonly Password _registrationPassword = new Password(_registrationPasswordAsString);
        private readonly Email _registrationEmail = Email.Parse("test.test@test.se");
        private readonly Guid _registrationAccountId = Guid.NewGuid();
        private MessageSpy _messageSpy;
        private Account _registered;

        [SetUp]
        public void RegisterAccount()
        {
            var container = DomainTestWiringHelper.SetupContainerForTesting();
            using(container.BeginScope())
            {
                using(var transaction = container.BeginTransactionalUnitOfWorkScope())
                {
                    _messageSpy = container.Resolve<MessageSpy>();
                    var duplicateAccountChecker = container.Resolve<IDuplicateAccountChecker>();
                    var accountRepository = container.Resolve<IAccountManagementEventStoreSession>();
                    _registered = Account.Register(_registrationEmail, _registrationPassword, _registrationAccountId, accountRepository, duplicateAccountChecker);
                    transaction.Commit();
                }
            }
        }

        [Test]
        public void AnIUserRegisteredAccountEventIsPublished()
        {
            _messageSpy.ReceivedMessages.OfType<IUserRegisteredAccountEvent>().ToList().Should().HaveCount(1);
        }

        [Test]
        public void AccountEmailIsTheOneUsedForRegistration()
        {
            Assert.That(_registrationEmail, Is.EqualTo(_registered.Email));
        }

        [Test]
        public void AccountPasswordIsTheOnUsedForRegistration()
        {
            Assert.True(_registered.Password.IsCorrectPassword(_registrationPasswordAsString));
        }
    }
}
