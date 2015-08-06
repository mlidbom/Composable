using Composable.System.Reflection;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEvestStoreEventSerializer
    {
        public static readonly JsonSerializerSettings JsonSettings = NewtonSoft.JsonSettings.JsonSerializerSettings;

        public string Serialize(object @event)
        {
            return JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings);
        }

        public IAggregateRootEvent Deserialize(string eventType, string eventData)
        {
            return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, eventType.AsType(), JsonSettings);
        }
    }
}