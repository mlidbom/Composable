using System;
using System.Text.RegularExpressions;
using Composable.Messaging;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.System;
using Newtonsoft.Json;

namespace Composable.Serialization
{

    class RenamingSupportingJsonSerializer : IJsonSerializer
    {
        readonly JsonSerializerSettings _jsonSettings;
        readonly RenamingDecorator _renamingDecorator;
        protected internal RenamingSupportingJsonSerializer(JsonSerializerSettings jsonSettings, ITypeMapper typeMapper)
        {
            _jsonSettings = jsonSettings;
            _renamingDecorator = new RenamingDecorator(typeMapper);
        }

        public string Serialize(object instance)
        {
            var json = JsonConvert.SerializeObject(instance, Formatting.Indented, _jsonSettings);
            json = _renamingDecorator.ReplaceTypeNames(json);
            return json;
        }

        public object Deserialize(Type type, string json)
        {
            json = _renamingDecorator.RestoreTypeNames(json);
            return JsonConvert.DeserializeObject(json, type, _jsonSettings)!;
        }
    }

    class RenamingDecorator
    {
        readonly ITypeMapper _typeMapper;

        static readonly OptimizedLazy<Regex> FindTypeNames = new OptimizedLazy<Regex>(() => new Regex(@"""\$type""\: ""([^""]*)""", RegexOptions.Compiled));

        public RenamingDecorator(ITypeMapper typeMapper) => _typeMapper = typeMapper;

        public string ReplaceTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeNamesWithTypeIds);

        string ReplaceTypeNamesWithTypeIds(Match match)
        {
            var type = Type.GetType(match.Groups[1].Value);
            var typeId = _typeMapper.GetId(type!);
            return $@"""$type"": ""{typeId.GuidValue}""";
        }

        public string RestoreTypeNames(string json) => FindTypeNames.Value.Replace(json, ReplaceTypeIdsWithTypeNames);

        string ReplaceTypeIdsWithTypeNames(Match match)
        {
            var typeId = new TypeId(Guid.Parse(match.Groups[1].Value));
            var type = _typeMapper.GetType(typeId);
            return $@"""$type"": ""{type.AssemblyQualifiedName}""";
        }
    }


    class EventStoreSerializer : IEventStoreSerializer
    {
        internal static readonly JsonSerializerSettings JsonSettings = Serialization.JsonSettings.SqlEventStoreSerializerSettings;

        readonly RenamingSupportingJsonSerializer _serializer;

        public EventStoreSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(JsonSettings, typeMapper);

        public string Serialize(AggregateEvent @event) => _serializer.Serialize(@event);
        public IAggregateEvent Deserialize(Type eventType, string json) => (IAggregateEvent)_serializer.Deserialize(eventType, json);
    }

    class DocumentDbSerializer : RenamingSupportingJsonSerializer, IDocumentDbSerializer
    {
        public DocumentDbSerializer(ITypeMapper typeMapper) : base(JsonSettings.SqlEventStoreSerializerSettings, typeMapper) {}
    }

    class RemotableMessageSerializer : IRemotableMessageSerializer
    {
        readonly RenamingSupportingJsonSerializer _serializer;

        public RemotableMessageSerializer(ITypeMapper typeMapper) => _serializer = new RenamingSupportingJsonSerializer(Serialization.JsonSettings.JsonSerializerSettings, typeMapper);

        public string SerializeResponse(object response) => _serializer.Serialize(response);
        public object DeserializeResponse(Type responseType, string json) => _serializer.Deserialize(responseType, json);

        public string SerializeMessage(MessageTypes.Remotable.IMessage message) => _serializer.Serialize(message);
        public MessageTypes.Remotable.IMessage DeserializeMessage(Type messageType, string json) => (MessageTypes.Remotable.IMessage)_serializer.Deserialize(messageType, json);
    }
}