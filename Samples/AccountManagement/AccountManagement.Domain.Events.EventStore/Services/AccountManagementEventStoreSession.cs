using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.Domain.Events.EventStore.Services
{
    //Creating your own subtypes of these interfaces makes it easy to set up wiring for multiple sysems that each might need to wire these services.
    //Should you use the base interfaces directly when wiring it will essentially be blind luck which registration is used at runtime if you wire
    //services from multiple systems. So best practice is to always create and wire subtypes of services like IEventStoreSession and IEventstoreReader
    public interface IAccountManagementEventStoreReader : IEventStoreReader { }
    public interface IAccountManagementEventStoreSession : IAccountManagementEventStoreReader, IEventStoreSession { }

    class AccountManagementEventStoreSession : EventStoreSession, IAccountManagementEventStoreSession
    {
        public AccountManagementEventStoreSession(IServiceBus bus, IEventStore store, ISingleContextUseGuard usageGuard, IUtcTimeTimeSource timeSource)
            : base(bus, store, usageGuard, timeSource) {}
    }
}
