using Composable.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Events
{
    public static partial class AccountEvent
    {
        public interface Root : IAggregateEvent {}

        public interface Created : Root, IAggregateCreatedEvent
            //Used in multiple places by the infrastructure and clients. Things WILL BREAK without this.
            //Aggregate: Sets the ID when such an event is raised.
            //Creates a viewmodel automatically when received by an SingleAggregateQueryModelUpdater
        {}


        public interface UserRegistered :
            Created,
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
                Passwords.Password Password { get; /* Never add a setter! */ }
            }

            public interface Email : AccountEvent.Root
            {
                Domain.Email Email { get; /* Never add a setter! */ }
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
