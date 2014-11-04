using System;
using Composable.CQRS.EventSourcing;

namespace CQRS.Tests.CQRS.EventSourcing
{
    internal class User : AggregateRoot<User, AggregateRootEvent>
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
}