using System;
using Composable.Persistence.EventStore;
using Newtonsoft.Json;

namespace Composable.Serialization
{

    abstract class NewtonSoftSerializer : IJsonSerializer
    {
        readonly JsonSerializerSettings _jsonSettings;
        protected NewtonSoftSerializer(JsonSerializerSettings jsonSettings) => _jsonSettings = jsonSettings;

        public string Serialize(object @event) => JsonConvert.SerializeObject(@event, Formatting.Indented, _jsonSettings);

        public IAggregateEvent Deserialize(Type eventType, string eventData) => (IAggregateEvent)JsonConvert.DeserializeObject(eventData, eventType, _jsonSettings);
    }

    class NewtonSoftEventStoreSerializer : NewtonSoftSerializer, IEventStoreSerializer
    {
        public static readonly JsonSerializerSettings JsonSettings = Composable.NewtonSoft.JsonSettings.SqlEventStoreSerializerSettings;
        public NewtonSoftEventStoreSerializer() : base(JsonSettings) {}
    }

    class NewtonDocumentDbSerializer : NewtonSoftSerializer, IDocumentDbSerializer
    {
        public static readonly JsonSerializerSettings JsonSettings = Composable.NewtonSoft.JsonSettings.SqlEventStoreSerializerSettings;
        public NewtonDocumentDbSerializer() : base(JsonSettings) {}
    }

    class NewtonBusMessageSerializer : NewtonSoftSerializer, IBusMessageSerializer
    {
        public static readonly JsonSerializerSettings JsonSettings = Composable.NewtonSoft.JsonSettings.SqlEventStoreSerializerSettings;
        public NewtonBusMessageSerializer() : base(JsonSettings) {}
    }
}