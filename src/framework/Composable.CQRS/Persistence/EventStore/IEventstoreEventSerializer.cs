using System;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    interface IEventStoreEventSerializer
    {
        string Serialize(object @event);
        IDomainEvent Deserialize(Type eventType, string eventData);
    }
}