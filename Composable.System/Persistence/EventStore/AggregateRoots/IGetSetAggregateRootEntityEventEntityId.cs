namespace Composable.Persistence.EventStore.AggregateRoots
{
    public interface IGetSetAggregateRootEntityEventEntityId<TEntityId, in TEventClass, in TEventInterface> : IGetAggregateRootEntityEventEntityId<TEventInterface, TEntityId>
    {
        void SetEntityId(TEventClass @event, TEntityId id);
    }
}