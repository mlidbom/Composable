namespace Composable.Persistence.EventStore.AggregateRoots
{
    public interface IGetAggregateRootEntityEventEntityId<TEventInterface, TEntityId>
    {
        TEntityId GetId(TEventInterface @event);
    }
}