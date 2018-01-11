using System;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.Tests.CQRS.EventRefactoring.Migrations;
using JetBrains.Annotations;

namespace Composable.Tests.CQRS
{
    class User : AggregateRoot<User,UserEvent, IUserEvent>
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

    [TypeId("602DB8BE-9210-423B-999E-7B0F21461BB8")]interface IUserEvent : IAggregateRootEvent, IRootEvent
    { }

    abstract class UserEvent : AggregateRootEvent, IUserEvent
    {
        protected UserEvent() {}
        protected UserEvent(Guid aggregateRootId) : base(aggregateRootId) {}
    }

    [TypeId("EEF29472-92ED-4AB4-81D3-3B38EB571025")]class UserChangedEmail : UserEvent, IUserEvent
    {
        public UserChangedEmail(string email) => Email = email;
        public string Email { get; private set; }
    }

    [TypeId("B10B3CA4-5F9D-43FF-B2BB-DA65DFBAF3BD")]class UserChangedPassword : UserEvent, IUserEvent
    {
        public UserChangedPassword(string password) => Password = password;
        public string Password { get; private set; }
    }

    [TypeId("B9517AE2-0697-469C-82C4-A3C827F11142")]class UserRegistered : UserEvent, IAggregateRootCreatedEvent
    {
        public UserRegistered(Guid userId, string email, string password):base(userId)
        {
            Email = email;
            Password = password;
        }

        public string Email { get; private set; }
        public string Password { get; private set; }
    }

    [TypeId("D04B3EE8-8C5D-4CF5-BA7E-8F6248EC91DA")][UsedImplicitly] class MigratedBeforeUserRegisteredEvent : UserEvent, IAggregateRootCreatedEvent
    {
    }

    [TypeId("49ACCFAC-3F2C-4D8D-A199-233298653B40")][UsedImplicitly] class MigratedAfterUserChangedEmailEvent : UserEvent, IAggregateRootCreatedEvent
    {
    }

    [TypeId("51AAB04D-31BB-4060-BBA6-EC37A9BB06D4")][UsedImplicitly] class MigratedReplaceUserChangedPasswordEvent : UserEvent, IAggregateRootCreatedEvent
    {
    }
}