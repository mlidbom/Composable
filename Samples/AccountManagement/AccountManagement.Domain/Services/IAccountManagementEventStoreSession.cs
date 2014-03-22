using Composable.CQRS.EventSourcing;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.Domain.Services
{
    public interface IAccountManagementEventStoreSession : IEventStoreSession {}

    public class AccountManagementEventStoreSession : EventStoreSession, IAccountManagementEventStoreSession
    {
        public AccountManagementEventStoreSession(IServiceBus bus, IEventStore store, ISingleContextUseGuard usageGuard) 
            : base(bus, store, usageGuard) {}
    }
}
