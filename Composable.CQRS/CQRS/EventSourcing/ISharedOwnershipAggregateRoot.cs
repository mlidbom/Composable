namespace Composable.CQRS.EventSourcing
{
    public interface ISharedOwnershipAggregateRoot
    {
        void IntegrateExternallyRaisedEvent(IAggregateRootEvent evt);
    }
}