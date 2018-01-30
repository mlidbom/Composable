using System;
using Composable.Messaging;
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
        string Serialize(AggregateEvent @event);
        IAggregateEvent Deserialize(Type eventType, string json);
    }

    interface IDocumentDbSerializer
    {
        string Serialize(object instance);
        object Deserialize(Type eventType, string json);
    }

    interface IRemotableMessageSerializer
    {
        string Serialize(BusApi.Remotable.IMessage message);
        BusApi.Remotable.IMessage Deserialize(Type eventType, string json);
    }
}