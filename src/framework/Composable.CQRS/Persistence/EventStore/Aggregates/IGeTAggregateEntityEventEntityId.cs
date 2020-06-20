using System.Diagnostics.CodeAnalysis;

namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IGetAggregateEntityEventEntityId<in TEvent, out TEntityId>
    {
        TEntityId GetId(TEvent @event);
    }
}