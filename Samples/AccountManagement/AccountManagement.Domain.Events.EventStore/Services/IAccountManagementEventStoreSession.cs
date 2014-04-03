using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain.Events.EventStore.Services
{
    public interface IAccountManagementEventStoreSession : IAccountManagementEventStoreReader, IEventStoreSession {}
}
