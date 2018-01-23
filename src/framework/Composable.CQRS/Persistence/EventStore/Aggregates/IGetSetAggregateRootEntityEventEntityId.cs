namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IGetSeTAggregateEntityEventEntityId<TEntityId, in TEventClass, in TEventInterface> : IGeTAggregateEntityEventEntityId<TEventInterface, TEntityId>
    {
        void SetEntityId(TEventClass @event, TEntityId id);
    }
}