namespace Composable.CQRS.CQRS.EventSourcing
{
    public interface IGetAggregateRootEntityEventEntityId<TEventInterface, TEntityId>
    {
        TEntityId GetId(TEventInterface @event);
    }
}