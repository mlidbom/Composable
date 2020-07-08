namespace Composable.Persistence.EventStore
{
    interface IAggregateTypeValidator
    {
        void AssertIsValid<TAggregate>();
    }
}
