using Composable.Persistence.EventStore;

namespace AccountManagement.Domain.Services
{
    //Creating your own subtypes of these interfaces makes it easy to set up wiring for multiple sysems that each might need to wire these services.
    //Should you use the base interfaces directly when wiring it will essentially be blind luck which registration is used at runtime if you wire
    //services from multiple systems. So we force you to be safe and provide your own types.
    public interface IAccountManagementEventStoreReader : IEventStoreReader { }
    interface IAccountManagementEventStoreUpdater : IAccountManagementEventStoreReader, IEventStoreUpdater { }
}
