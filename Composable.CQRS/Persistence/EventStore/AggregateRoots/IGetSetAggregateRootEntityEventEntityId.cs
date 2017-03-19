namespace Composable.Persistence.EventStore.AggregateRoots
{
    public interface IGetSetAggregateRootEntityEventEntityId<TEntityId, TEventClass, TEventInterface> : IGetAggregateRootEntityEventEntityId<TEventInterface, TEntityId>
    {
        void SetEntityId(TEventClass @event, TEntityId id);
    }
}