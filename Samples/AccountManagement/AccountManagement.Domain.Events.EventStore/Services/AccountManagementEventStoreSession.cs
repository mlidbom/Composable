using Composable.CQRS.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.Domain.Events.EventStore.Services
{
    //Creating your own subtypes of these interfaces makes it easy to set up wiring for multiple sysems that each might need to wire these services.
    //Should you use the base interfaces directly when wiring it will essentially be blind luck which registration is used at runtime if you wire
    //services from multiple systems. So we force you to be safe and provide your own types.
    interface IAccountManagementEventStoreReader : IEventStoreReader { }
    interface IAccountManagementEventStoreSession : IAccountManagementEventStoreReader, IEventStoreSession { }    
}
