using System;

namespace Composable.Persistence.EventStore
{
    interface IEventStoreEventSerializer
    {
        string Serialize(object @event);
        IAggregateEvent Deserialize(Type eventType, string eventData);
    }
}