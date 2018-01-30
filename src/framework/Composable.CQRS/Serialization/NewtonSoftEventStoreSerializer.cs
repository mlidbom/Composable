using System;
using System.Collections.Generic;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Newtonsoft.Json;

namespace Composable.Serialization
{

    class RenamingSupportingJsonSerializer : IJsonSerializer
    {
        readonly JsonSerializerSettings _jsonSettings;
        readonly TypeMapper _typeMapper;
        readonly RenamingDecorator _renamingDecorator;
        protected internal RenamingSupportingJsonSerializer(JsonSerializerSettings jsonSettings, TypeMapper typeMapper)
        {
            _jsonSettings = jsonSettings;
            _renamingDecorator = new RenamingDecorator(_typeMapper);
            _typeMapper = typeMapper;
        }

        public string Serialize(object @event)
        {
            var json = JsonConvert.SerializeObject(@event, Formatting.Indented, _jsonSettings);
            json = _renamingDecorator.ReplaceTypeNames(json);
            return json;
        }

        public IAggregateEvent Deserialize(Type eventType, string eventData)
        {
            eventData = _renamingDecorator.RestoreTypeNames(eventData);
            return (IAggregateEvent)JsonConvert.DeserializeObject(eventData, eventType, _jsonSettings);
        }
    }

    class RenamingDecorator
    {
        readonly IReadOnlyList<string> NoTypeNames = new List<string>();
        readonly TypeMapper _typeMapper;
        public RenamingDecorator(TypeMapper typeMapper) => _typeMapper = typeMapper;


        public string ReplaceTypeNames(string json)
        {
            var typeNames = FindTypeNames(json);

            return json;
        }

        IReadOnlyList<string> FindTypeNames(string json)
        {
            if(json.IndexOf("$type:") == -1)
            {
                return NoTypeNames;
            }

            return NoTypeNames;
        }

        public string RestoreTypeNames(string json)
        {
            return json;
        }
    }


    class NewtonSoftEventStoreSerializer : RenamingSupportingJsonSerializer, IEventStoreSerializer
    {
        public static readonly JsonSerializerSettings JsonSettings = Serialization.JsonSettings.SqlEventStoreSerializerSettings;
        public NewtonSoftEventStoreSerializer(TypeMapper typeMapper) : base(JsonSettings, typeMapper) {}
    }

    class NewtonDocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
    {
        public NewtonDocumentDbSerializer(TypeMapper typeMapper) : base(JsonSettings.SqlEventStoreSerializerSettings, typeMapper) {}
    }

    class NewtonBusMessageSerializer : RenamingSupportingJsonSerializer, IBusMessageSerializer
    {
        public NewtonBusMessageSerializer(TypeMapper typeMapper) : base(JsonSettings.SqlEventStoreSerializerSettings, typeMapper) {}
    }
}