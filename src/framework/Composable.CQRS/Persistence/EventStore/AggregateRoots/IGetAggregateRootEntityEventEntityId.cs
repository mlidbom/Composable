namespace Composable.Persistence.EventStore.AggregateRoots
{
    public interface IGetAggregateRootEntityEventEntityId<in TEventInterface, out TEntityId>
    {
        TEntityId GetId(TEventInterface @event);
    }
}