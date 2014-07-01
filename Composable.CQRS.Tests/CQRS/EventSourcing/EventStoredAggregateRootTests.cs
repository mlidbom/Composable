using System;
using System.Linq;
using Composable.CQRS.EventSourcing;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.CQRS.EventSourcing
{
    public class EventStoredAggregateRootTests
    {
        [Test]
        public void ApplyAs_should_apply_by_base_event_handler()
        {
            // Arrange
            var admin = new Administrator();

            // Act
            admin.Register("admin@gmail.com");

            // Assert
            admin.Email.Should().Be("admin@gmail.com");
        }

        [Test]
        public void ApplyAs_should_just_add_actual_event_to_changes_list()
        {
            // Arrange
            var admin = new Administrator();

            // Act
            admin.Register("admin@gmail.com");
            var evnetStore = admin as IEventStored;

            // Assert
            evnetStore.GetChanges().First().Should().BeOfType<AdministratorRegisteredEvent>();
            evnetStore.GetChanges().Count().Should().Be(1);
        }
    }

    public interface IAccountRegisteredEvent : IAggregateRootEvent
    {
        string Email { get; }
    }

    public class AdministratorRegisteredEvent : IAccountRegisteredEvent
    {
        public Guid EventId { get; set; }
        public int AggregateRootVersion { get; set; }
        public Guid AggregateRootId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Email { get; set; }
    }

    public abstract class Account : EventStoredAggregateRoot<Account>
    {
        public string Email { get; private set; }

        protected Account()
        {
            Register(Handler.For<IAccountRegisteredEvent>().OnApply(e =>
                                                                    {
                                                                        SetIdBeVerySureYouKnowWhatYouAreDoing(e.AggregateRootId);
                                                                        Email = e.Email;
                                                                    }));
        }

        public void Register(string email)
        {
            ApplyEvent(CreateRegisteredEvent(email));
        }

        protected abstract IAccountRegisteredEvent CreateRegisteredEvent(string email);
    }

    public class Administrator : Account
    {
        public Administrator()
        {
            Register(Handler.For<AdministratorRegisteredEvent>().OnApply(ApplyAs<IAccountRegisteredEvent>));
        }

        protected override IAccountRegisteredEvent CreateRegisteredEvent(string email)
        {
            return new AdministratorRegisteredEvent
                   {
                       AggregateRootId = Guid.NewGuid(),
                       Email = email
                   };
        }
    }
}
