using System;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Tests.CQRS.EventRefactoring.Migrations;
using JetBrains.Annotations;

namespace Composable.Tests.CQRS
{
    class User : Aggregate<User,UserEvent, IUserEvent>
    {
        public string Email { get; private set; }
        public string Password { get; private set; }


        public User():base(new DateTimeNowTimeSource())
        {
            RegisterEventAppliers()
                .For<UserRegistered>(e =>
                                     {
                                         Email = e.Email;
                                         Password = e.Password;
                                     })
                .For<UserChangedEmail>(e => Email = e.Email)
                .For<MigratedBeforeUserRegisteredEvent>(e => {})
                .For<MigratedAfterUserChangedEmailEvent>(e => {})
                .For<MigratedReplaceUserChangedPasswordEvent>(e => {})
                .For<UserChangedPassword>(e => Password = e.Password);
        }

        public void Register(string email, string password, Guid id)
        {
            Publish(new UserRegistered(id, email, password));
        }

        public static User Register(IEventStoreUpdater aggregates, string email, string password, Guid id)
        {
            var user = new User();
            user.Register(email, password, id);
            aggregates.Save(user);
            return user;
        }

        public void ChangePassword(string password)
        {
            Publish(new UserChangedPassword(password));
        }

        public void ChangeEmail(string email)
        {
            Publish(new UserChangedEmail(email));
        }
    }

    interface IUserEvent : IAggregateEvent, IRootEvent
    { }

    abstract class UserEvent : AggregateEvent, IUserEvent
    {
        protected UserEvent() {}
        protected UserEvent(Guid aggregateId) : base(aggregateId) {}
    }

    class UserChangedEmail : UserEvent, IUserEvent
    {
        public UserChangedEmail(string email) => Email = email;
        public string Email { get; private set; }
    }

    class UserChangedPassword : UserEvent, IUserEvent
    {
        public UserChangedPassword(string password) => Password = password;
        public string Password { get; private set; }
    }

    class UserRegistered : UserEvent, IAggregateCreatedEvent
    {
        public UserRegistered(Guid userId, string email, string password):base(userId)
        {
            Email = email;
            Password = password;
        }

        public string Email { get; private set; }
        public string Password { get; private set; }
    }

    [UsedImplicitly] class MigratedBeforeUserRegisteredEvent : UserEvent, IAggregateCreatedEvent
    {
    }

    [UsedImplicitly] class MigratedAfterUserChangedEmailEvent : UserEvent, IAggregateCreatedEvent
    {
    }

    [UsedImplicitly] class MigratedReplaceUserChangedPasswordEvent : UserEvent, IAggregateCreatedEvent
    {
    }
}