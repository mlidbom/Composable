using Composable.Persistence.EventStore;

namespace TutorialCode
{
    public static partial class AccountEvent
    {
        public interface Root : IAggregateEvent {}

        public interface Registered : Root,
                                      PropertyUpdated.Email,
                                      PropertyUpdated.Password,
                                      IAggregateCreatedEvent {}

        public interface NewEmailEntered : Root
        {
            string NewEmail { get; }
        }

        public interface NewEmailValidated : PropertyUpdated.Email {}

        public interface UserChangedPassword : PropertyUpdated.Password {}

        public static class PropertyUpdated
        {
            public interface Password : Root
            {
                string Password { get; }
            }

            public interface Email : Root
            {
                string Email { get; }
            }
        }
    }
}
