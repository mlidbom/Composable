namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IGetAggregateRootEntityEventEntityId<in TEventInterface, out TEntityId>
    {
        TEntityId GetId(TEventInterface @event);
    }
}