using System;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
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
                .For<IUserRegistered>(e =>
                                     {
                                         Email = e.Email;
                                         Password = e.Password;
                                     })
                .For<IUserChangedEmail>(e => Email = e.Email)
                .For<IMigratedBeforeUserRegisteredEvent>(e => {})
                .For<IMigratedAfterUserChangedEmailEvent>(e => {})
                .For<IMigratedReplaceUserChangedPasswordEvent>(e => {})
                .For<IUserChangedPassword>(e => Password = e.Password);
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

    interface IUserEvent : IAggregateEvent {}

    abstract class UserEvent : AggregateEvent, IUserEvent
    {
        protected UserEvent() {}
        protected UserEvent(Guid aggregateId) : base(aggregateId) {}
    }

    interface IUserChangedEmail : IUserEvent
    {
        string Email { get; }
    }
    class UserChangedEmail : UserEvent, IUserChangedEmail
    {
        public UserChangedEmail(string email) => Email = email;
        public string Email { get; private set; }
    }

    interface IUserChangedPassword : IUserEvent
    {
        string Password { get; }
    }

    class UserChangedPassword : UserEvent, IUserChangedPassword
    {
        public UserChangedPassword(string password) => Password = password;
        public string Password { get; private set; }
    }

    interface IUserRegistered : IUserEvent, IAggregateCreatedEvent
    {
        string Email { get; }
        string Password { get; }
    }

    class UserRegistered : UserEvent, IUserRegistered
    {
        public UserRegistered(Guid userId, string email, string password) : base(userId)
        {
            Email = email;
            Password = password;
        }

        public string Email { get; private set; }
        public string Password { get; private set; }
    }

    interface IMigratedBeforeUserRegisteredEvent : IUserEvent, IAggregateCreatedEvent {}
    [UsedImplicitly] class MigratedBeforeUserRegisteredEvent : UserEvent, IMigratedBeforeUserRegisteredEvent {}

    interface IMigratedAfterUserChangedEmailEvent : IUserEvent, IAggregateCreatedEvent {}
    [UsedImplicitly] class MigratedAfterUserChangedEmailEvent : UserEvent, IMigratedAfterUserChangedEmailEvent {}

    interface IMigratedReplaceUserChangedPasswordEvent : IUserEvent, IAggregateCreatedEvent {}
    [UsedImplicitly] class MigratedReplaceUserChangedPasswordEvent : UserEvent, IMigratedReplaceUserChangedPasswordEvent {}
}