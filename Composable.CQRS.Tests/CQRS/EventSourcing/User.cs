using System;
using Composable.CQRS.EventSourcing;

namespace CQRS.Tests.CQRS.EventSourcing
{
    internal class User : AggregateRoot<User,UserEvent, IUserEvent>
    {
        public string Email { get; private set; }
        public string Password { get; private set; }


        public User()
        {
            RegisterEventAppliers()
                .For<UserRegistered>(e =>
                                     {
                                         Email = e.Email;
                                         Password = e.Password;
                                     })
                .For<UserChangedEmail>(e => Email = e.Email)
                .For<UserChangedPassword>(e => Password = e.Password);
        }

        public void Register(string email, string password, Guid id)
        {
            RaiseEvent(new UserRegistered() { AggregateRootId = id, UserId = id, Email = email, Password = password});
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


    public interface IUserEvent : IAggregateRootEvent
    {}

    public abstract class UserEvent : AggregateRootEvent, IUserEvent
    {
        protected UserEvent() {}
        protected UserEvent(Guid aggregateRootId) : base(aggregateRootId) {}
    }

    public class UserChangedEmail : UserEvent, IUserEvent
    {
        public UserChangedEmail(string email)
        {
            Email = email;
        }
        public string Email { get; private set; }
    }

    public class UserChangedPassword : UserEvent, IUserEvent
    {
        public string Password { get; set; }
    }

    public class UserRegistered : UserEvent, IAggregateRootCreatedEvent
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}