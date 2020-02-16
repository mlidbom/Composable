using System.Diagnostics.CodeAnalysis;

namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IGetAggregateEntityEventEntityId<in TEvent, out TEntityId>
    {
        [return:MaybeNull]TEntityId GetId(TEvent @event);
    }
}