using System;
using Composable.Persistence.EventSourcing;
using Newtonsoft.Json;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    static class SqlServerEvestStoreEventSerializer
    {
        internal static readonly JsonSerializerSettings JsonSettings = NewtonSoft.JsonSettings.SqlEventStoreSerializerSettings;

        public static string Serialize(object @event) => JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings);

        public static IAggregateRootEvent Deserialize(Type eventType, string eventData) => (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, eventType, JsonSettings);
    }
}