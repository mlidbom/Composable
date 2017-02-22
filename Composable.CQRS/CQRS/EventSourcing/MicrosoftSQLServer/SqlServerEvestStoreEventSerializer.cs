using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    public class SqlServerEvestStoreEventSerializer
    {
        public static JsonSerializerSettings JsonSettings => NewtonSoft.JsonSettings.SqlEventStoreSerializerSettings;

        public string Serialize(object @event)
        {
            return JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings);
        }

        public IAggregateRootEvent Deserialize(Type eventType, string eventData)
        {
            return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, eventType, JsonSettings);
        }
    }

    class SqlServerDebugEventStoreEventSerializer
    {
        public class DebugPrintingContractsResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select(p => CreateProperty(p, memberSerialization))
                                .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props;
            }            
        }

        public static JsonSerializerSettings JsonSettings =>
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new DebugPrintingContractsResolver(),
                Error = (serializer, err) => err.ErrorContext.Handled = true
    };

        public string Serialize(object @event, Formatting formatting)
        {
            return JsonConvert.SerializeObject(@event, formatting, JsonSettings);
        }
    }

    public static class AggregateRootEventDebugSerializer
    {
        public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented)
        {
            return $"{@this.GetType()}:{new SqlServerDebugEventStoreEventSerializer().Serialize(@this, formatting)}";
        }
    }
}