namespace Composable.Persistence.EventStore.PersistenceLayer
{
    class EventInsertionSpecification
    {
        public EventInsertionSpecification(IAggregateEvent @event) : this(@event, @event.AggregateVersion, @event.AggregateVersion) {}

        public EventInsertionSpecification(IAggregateEvent @event, int insertedVersion, int effectiveVersion)
        {
            Event = @event;
            InsertedVersion = insertedVersion;
            EffectiveVersion = effectiveVersion;
        }

        internal IAggregateEvent Event { get; }
        internal int InsertedVersion { get; }
        internal int EffectiveVersion { get; }
    }
}
