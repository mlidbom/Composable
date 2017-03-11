using System;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.GenericAbstractions.Time;
using JetBrains.Annotations;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
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
            RaiseEvent(new UserRegistered() { AggregateRootId = id, UserId = id, Email = email, Password = password});
        }

        public static User Register(IEventStoreSession aggregates, string email, string password, Guid id)
        {
            var user = new User();
            user.Register(email, password, id);
            aggregates.Save(user);
            return user;
        }

        public void ChangePassword(string password)
        {
            RaiseEvent(new UserChangedPassword() { Password = password });
        }

        public void ChangeEmail(string email)
        {
            RaiseEvent(new UserChangedEmail(email));
        }
    }

    interface IUserEvent : IAggregateRootEvent, IRootEvent
    { }

    abstract class UserEvent : AggregateRootEvent, IUserEvent
    {}

    class UserChangedEmail : UserEvent, IUserEvent
    {
        public UserChangedEmail(string email)
        {
            Email = email;
        }
        public string Email { get; private set; }
    }

    class UserChangedPassword : UserEvent, IUserEvent
    {
        public string Password { get; set; }
    }

    class UserRegistered : UserEvent, IAggregateRootCreatedEvent
    {
        public Guid UserId { [UsedImplicitly] get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [UsedImplicitly] class MigratedBeforeUserRegisteredEvent : UserEvent, IAggregateRootCreatedEvent
    {
    }

    [UsedImplicitly] class MigratedAfterUserChangedEmailEvent : UserEvent, IAggregateRootCreatedEvent
    {
    }

    [UsedImplicitly] class MigratedReplaceUserChangedPasswordEvent : UserEvent, IAggregateRootCreatedEvent
    {
    }
}