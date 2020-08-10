using System;

namespace Composable.Persistence.EventStore.PersistenceLayer
{
    class EventDataRow
    {
        public EventDataRow(IAggregateEvent @event, AggregateEventStorageInformation storageInformation, Guid eventType, string eventAsJson)
        {
            EventJson = eventAsJson;
            EventType = eventType;

            EventId = @event.MessageId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;

            StorageInformation = storageInformation;
        }

        public EventDataRow(EventInsertionSpecification specification, Guid typeId, string eventAsJson)
        {
            var @event = specification.Event;
            EventJson = eventAsJson;
            EventType = typeId;

            EventId = @event.MessageId;
            AggregateVersion = @event.AggregateVersion;
            AggregateId = @event.AggregateId;
            UtcTimeStamp = @event.UtcTimeStamp;

            StorageInformation = new AggregateEventStorageInformation()
                                 {
                                     InsertedVersion = specification.InsertedVersion,
                                     EffectiveVersion = specification.EffectiveVersion
                                 };
        }

        public EventDataRow(Guid eventType, string eventJson, Guid eventId, int aggregateVersion, Guid aggregateId, DateTime utcTimeStamp, AggregateEventStorageInformation storageInformation)
        {
            EventType = eventType;
            EventJson = eventJson;
            EventId = eventId;
            AggregateVersion = aggregateVersion;
            AggregateId = aggregateId;
            UtcTimeStamp = utcTimeStamp;

            StorageInformation = storageInformation;
        }

        public Guid EventType { get; private set; }
        public string EventJson { get; private set; }
        public Guid EventId { get; private set; }
        public int AggregateVersion { get; private set; }

        public Guid AggregateId { get; private set; }
        public DateTime UtcTimeStamp { get; private set; }

        internal AggregateEventStorageInformation StorageInformation { get; private set; }

        public override string ToString() => $"{nameof(StorageInformation.InsertedVersion)}{StorageInformation.InsertedVersion},{nameof(StorageInformation.EffectiveVersion)}{StorageInformation.EffectiveVersion}, {nameof(StorageInformation.ReadOrder)}{StorageInformation.ReadOrder}";
    }
}
