using JetBrains.Annotations;

namespace Composable.CQRS.EventSourcing
{
    [UsedImplicitly]
    public interface IEventStore
    {
        IEventStoreSession OpenSession();
    }
}