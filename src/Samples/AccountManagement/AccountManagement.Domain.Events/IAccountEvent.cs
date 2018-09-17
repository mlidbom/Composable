using Composable.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

// tutorial tag::namespace[]
namespace AccountManagement.Domain.Events
{
    // tutorial tag::class[]
    public static partial class AccountEvent
    {
        public interface Root : IAggregateEvent {}

        public interface Created : Root, IAggregateCreatedEvent {}

        public interface UserRegistered :
            Created,
            PropertyUpdated.Email,
            PropertyUpdated.Password {}

        public interface UserChangedEmail :
            PropertyUpdated.Email {}

        public interface UserChangedPassword :
            PropertyUpdated.Password {}

        public interface LoginAttempted : Root {}

        public interface LoggedIn : LoginAttempted
        {
            string AuthenticationToken { get; }
        }

        public interface LoginFailed : LoginAttempted {}

        // tag::property-updated[]
        public static class PropertyUpdated
        {
            public interface Password : Root
            {
                Passwords.Password Password { get; }
            }

            public interface Email : Root
            {
                Domain.Email Email { get; }
            }
        }
        // end::property-updated[]
    }
    // tutorial end::class[]
}
// end::namespace[]
