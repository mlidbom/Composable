using System;
using Composable.Messaging;
using Composable.Persistence.EventStore;

namespace Composable.Serialization
{
    interface IJsonSerializer
    {
        string Serialize(object instance);
        object Deserialize(Type type, string json);
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
        string SerializeMessage(MessageTypes.IRemotableMessage message);
        MessageTypes.IRemotableMessage DeserializeMessage(Type messageType, string json);

        string SerializeResponse(object response);
        object DeserializeResponse(Type responseType, string json);
    }
}