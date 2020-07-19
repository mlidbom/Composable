namespace Composable.Persistence.EventStore.Aggregates
{
    //Refactor: Consider removing this interface and having the aggregate component|entity pass actions as a constructor arguments to its base class instead.
    public interface IGetSetAggregateEntityEventEntityId<TEntityId, in TEventImplementation, in TEvent> : IGetAggregateEntityEventEntityId<TEvent, TEntityId>
    {
        void SetEntityId(TEventImplementation @event, TEntityId id);
    }
}