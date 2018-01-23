namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IGeTAggregateEntityEventEntityId<in TEvent, out TEntityId>
    {
        TEntityId GetId(TEvent @event);
    }
}