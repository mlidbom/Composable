namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IGeTAggregateEntityEventEntityId<in TEventInterface, out TEntityId>
    {
        TEntityId GetId(TEventInterface @event);
    }
}