using System;
using Composable.CQRS.EventSourcing;

namespace CQRS.Tests.CQRS.EventSourcing
{
    internal class User: AggregateRoot<User>
    {
        public string Email { get; set; }
        public string Password { get; set; }


        public User()
        {
            RegisterEventHandler<UserRegistered>(e =>
                                                     {
                                                         SetIdBeVerySureYouKnowWhatYouAreDoing(e.UserId);
                                                         Email = e.Email;
                                                         Password = e.Password;
                                                     });
            RegisterEventHandler<UserChangedEmail>(e => Email = e.Email);
            RegisterEventHandler<UserChangedPassword>(e => Password = e.Password);
        }

        public void Register(string email, string password, Guid id)
        {
            ApplyEvent(new UserRegistered() { UserId = id, Email = email, Password = password});
        }

        public void ChangePassword(string password)
        {
            ApplyEvent(new UserChangedPassword(){Password = password});
        }

        public void ChangeEmail(string email)
        {
            ApplyEvent(new UserChangedEmail(email));
        }
    }
}