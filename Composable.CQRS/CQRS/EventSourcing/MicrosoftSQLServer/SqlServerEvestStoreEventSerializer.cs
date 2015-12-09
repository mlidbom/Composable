using System;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    internal class SqlServerEvestStoreEventSerializer
    {
        public static readonly JsonSerializerSettings JsonSettings = NewtonSoft.JsonSettings.SqlEventStoreSerializerSettings;

        public string Serialize(object @event)
        {
            return JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings);
        }

        public IAggregateRootEvent Deserialize(Type eventType, string eventData)
        {
            return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, eventType, JsonSettings);
        }
    }
}