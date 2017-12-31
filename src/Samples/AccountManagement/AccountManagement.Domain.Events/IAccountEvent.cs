using Composable;
using Composable.Messaging;
using Composable.Persistence.EventStore;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable InconsistentNaming

namespace AccountManagement.Domain.Events
{
    public static partial class AccountEvent
    {
        [TypeId("F6747983-2552-4952-8932-360E006FF836")]public interface Root : IDomainEvent {}

        [TypeId("D7599E28-936E-45AE-AD01-AED9EBFEB383")]public interface Created :
                Root,
                IAggregateRootCreatedEvent
            //Used in multiple places by the infrastructure and clients. Things WILL BREAK without this.
            //AggregateRoot: Sets the ID when such an event is raised.
            //Creates a viewmodel automatically when received by an SingleAggregateQueryModelUpdater
        {}


        [TypeId("6ED80573-E931-4956-94C6-947789963B89")]public interface UserRegistered :
            Created,
            PropertyUpdated.Email,
            PropertyUpdated.Password {}

        [TypeId("24F2D611-4973-4E85-8F39-E8DCBC43F212")]public interface UserChangedEmail :
            PropertyUpdated.Email {}

        [TypeId("23B46CCF-7FF6-4C90-ABE4-1CC81EE8952B")]public interface UserChangedPassword :
            PropertyUpdated.Password {}

        public static class PropertyUpdated
        {
            [TypeId("0695EF05-294B-493E-8D48-24966D839AC7")]public interface Password : AccountEvent.Root
            {
                Domain.Password Password { get; /* Never add a setter! */ }
            }

            [TypeId("F5CEE1C6-E258-4532-A91B-517859DB2F44")]public interface Email : AccountEvent.Root
            {
                Domain.Email Email { get; /* Never add a setter! */ }
            }
        }

        [TypeId("31173848-8ACC-45E9-8691-D9A8F0220169")]public interface LoggedIn : AccountEvent.Root
        {
            string AuthenticationToken { get; }
        }

        [TypeId("61A7BBF7-4491-4A2A-B915-25E03417C5F1")]public interface LoginFailed : AccountEvent.Root
        {
        }
    }
}
