using System;
using Composable.Persistence.EventStore;

namespace Composable.Serialization
{
    interface IJsonSerializer
    {
        string Serialize(object instance);
        object Deserialize(Type eventType, string json);
    }

    interface IEventStoreSerializer
    {
        string Serialize(object @event);
        IAggregateEvent Deserialize(Type eventType, string json);
    }

    interface IDocumentDbSerializer : IJsonSerializer
    {
    }

    interface IBusMessageSerializer : IJsonSerializer
    {
    }
}