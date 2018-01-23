namespace Composable.Persistence.EventStore.Aggregates
{
    public interface IGetSetAggregateEntityEventEntityId<TEntityId, in TEventImplementation, in TEvent> : IGetAggregateEntityEventEntityId<TEvent, TEntityId>
    {
        void SetEntityId(TEventImplementation @event, TEntityId id);
    }
}