using System;
using Composable.System.Reflection;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal class SqlServerEvestStoreEventSerializer
    {
        public static readonly JsonSerializerSettings JsonSettings = NewtonSoft.JsonSettings.JsonSerializerSettings;

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