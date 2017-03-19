using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer {
    static class SqlServerDebugEventStoreEventSerializer
    {
        class DebugPrintingContractsResolver : DefaultContractResolver
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

        static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new DebugPrintingContractsResolver(),
                Error = (serializer, err) => err.ErrorContext.Handled = true
            };

        public static string Serialize(object @event, Formatting formatting) => JsonConvert.SerializeObject(@event, formatting, JsonSettings);
    }
}