using System;
using Composable.Persistence.EventStore;

namespace Composable.Serialization
{
    interface IJsonSerializer
    {
        string Serialize(object @event);
        IAggregateEvent Deserialize(Type eventType, string eventData);
    }

    interface IEventStoreSerializer : IJsonSerializer
    {
    }

    interface IDocumentDbSerializer : IJsonSerializer
    {
    }

    interface IBusMessageSerializer : IJsonSerializer
    {
    }
}