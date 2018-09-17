using Composable.Persistence.EventStore;

namespace TutorialCode
{
    public static partial class AccountEvent
    {
        public interface Root : IAggregateEvent {}

        public interface Created : AccountEvent.Root, IAggregateCreatedEvent{}

        public interface UserRegistered :
            AccountEvent.Created,
            PropertyUpdated.Email,
            PropertyUpdated.Password {}

        public interface UserChangedEmail :
            PropertyUpdated.Email {}

        public interface UserChangedPassword :
            PropertyUpdated.Password {}

        public static class PropertyUpdated
        {
            public interface Password : AccountEvent.Root
            {
                string Password { get; }
            }

            public interface Email : AccountEvent.Root
            {
                string Email { get; }
            }
        }

        public interface LoginAttempted : AccountEvent.Root
        {
        }

        public interface LoggedIn : LoginAttempted
        {
            string AuthenticationToken { get; }
        }

        public interface LoginFailed : LoginAttempted
        {
        }
    }
}
