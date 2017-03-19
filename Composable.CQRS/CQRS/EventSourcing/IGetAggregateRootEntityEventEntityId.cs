namespace Composable.CQRS.EventSourcing
{
    public interface IGetAggregateRootEntityEventEntityId<TEventInterface, TEntityId>
    {
        TEntityId GetId(TEventInterface @event);
    }
}