using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using Composable.NewtonSoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

    internal class SqlServerDebugEventStoreEventSerializer
    {
        public class DebugPrintingContractsResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select(p => CreateProperty(p, memberSerialization))
                                .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);


                if(property.PropertyType == typeof(SqlDecimal))
                {
                    property.Converter = new SqlDecimalConverter();
                }


                return property;

            }
        }

        public static readonly JsonSerializerSettings JsonSettings =
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

        public IAggregateRootEvent Deserialize(Type eventType, string eventData)
        {
            return (IAggregateRootEvent)JsonConvert.DeserializeObject(eventData, eventType, JsonSettings);
        }
    }

    internal class SqlDecimalConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object valuee, JsonSerializer serializer)
        {
            var value = (SqlDecimal)valuee;
            if(value.IsNull)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.Value.ToString());
            }
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) { throw new NotImplementedException(); }
        public override bool CanConvert(Type objectType) { throw new NotImplementedException(); }
    }

    internal static class AggregateRootEventDebugSerializer
    {
        public static string ToNewtonSoftDebugString(this object @this, Formatting formatting = Formatting.Indented)
        {
            return $"{@this.GetType()}:{new SqlServerDebugEventStoreEventSerializer().Serialize(@this, formatting)}";
        }
    }
}