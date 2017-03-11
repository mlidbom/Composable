namespace Composable.CQRS.CQRS.EventSourcing
{
    public interface IGetSetAggregateRootEntityEventEntityId<TEntityId, TEventClass, TEventInterface> : IGetAggregateRootEntityEventEntityId<TEventInterface, TEntityId>
    {
        void SetEntityId(TEventClass @event, TEntityId id);
    }
}