using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ReflectionCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Refactoring.Naming
{
    class TypeMapper : ITypeMapper, ITypeMappingRegistar
    {
        readonly IThreadShared<State> _state = ThreadShared.WithDefaultTimeout<State>();

        public TypeId GetId(Type type) => _state.Update(state =>
        {
            if(state.TypeToTypeIdMap.TryGetValue(type, out var typeId))
            {
                return typeId;
            }

            throw BuildExceptionDescribingHowToAddMissingMappings(new List<Type> {type});
        });

        public Type GetType(TypeId typeId) => _state.Update(state =>
        {
            if(state.TypeIdToTypeMap.TryGetValue(typeId, out var type))
            {
                return type;
            }

            throw new Exception($"Could not find type for {nameof(TypeId)}: {typeId}");
        });

        public bool TryGetType(TypeId typeId, [NotNullWhen(true)] out Type? type)
        {
            type = _state.Update(state => state.TypeIdToTypeMap.TryGetValue(typeId, out var innerType) ? innerType : null);

            return type != null;
        }

        public IEnumerable<TypeId> GetIdForTypesAssignableTo(Type type)
        {
            return _state.Update(state => state
                                                      .TypeToTypeIdMap
                                                      .Keys
                                                      .Where(type.IsAssignableFrom)
                                                      .Select(matchingType => state.TypeToTypeIdMap[matchingType])
                                                      .ToArray());
        }

        public void AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings) => _state.Update(state =>
        {
            var typesWithMissingMappings = typesThatRequireMappings.Where(type => !state.TypeToTypeIdMap.ContainsKey(type)).ToList();
            if(typesWithMissingMappings.Any())
            {
                throw BuildExceptionDescribingHowToAddMissingMappings(typesWithMissingMappings);
            }
        });

        public void IncludeMappingsFrom(TypeMapper other) => _state.Update(state => other._state.Update(state.IncludeMappingsFrom));

        public ITypeMappingRegistar Map<TType>(Guid typeIdGuid)
        {
            _state.Update(state => state.Map(typeof(TType), new TypeId(typeIdGuid)));
            return this;
        }

        public ITypeMappingRegistar Map<TType>(string typeGuid) => Map<TType>(Guid.Parse(typeGuid));

        static void AssertTypeValidForMapping(Type type)
        {
            if(type.IsAbstract)
            {
                if(!(typeof(IRemotableEvent).IsAssignableFrom(type)))
                {
                    throw new Exception($"Type: {type.FullName} is abstract and is not a {typeof(IRemotableEvent).FullName}. For other types you should only map concrete types.");
                }
            }
        }

        static Exception BuildExceptionDescribingHowToAddMissingMappings(List<Type> typesWithMissingMappings)
        {
            typesWithMissingMappings = typesWithMissingMappings.Distinct().OrderBy(type => type.GetFullNameCompilable()).ToList();

            var fixMessage = new StringBuilder();
            fixMessage.AppendLine($@"
In order to allow you to freely rename and move your types without breaking your persisted data you are required to map your types to Guid values that are used in place of your type names in the persisted data.
Some such required type mappings are missing. 
You should map them in your endpoint configuration by using {typeof(IEndpointBuilder)}.{nameof(IEndpointBuilder.TypeMapper)}")
                      .Append(StartOfMappingList);

            typesWithMissingMappings.ForEach(type => fixMessage.Append($"{Environment.NewLine}   .{MapMethodCallforType(type)}"));

            fixMessage.Append(';').AppendLine().AppendLine();

            return new Exception(fixMessage.ToString());
        }

        class State
        {
            public readonly Dictionary<Type, TypeId> TypeToTypeIdMap = new Dictionary<Type, TypeId>();
            public readonly Dictionary<TypeId, Type> TypeIdToTypeMap = new Dictionary<TypeId, Type>();

            internal void Map(Type type, TypeId typeId)
            {
                if(TypeToTypeIdMap.TryGetValue(type, out var existingTypeId))
                {
                    if(existingTypeId == typeId) return;
                    throw new Exception($"Attempted to map Type:{type.FullName} to: {typeId}, but it is already mapped to: TypeId: {existingTypeId}");
                }

                if(TypeIdToTypeMap.TryGetValue(typeId, out var existingType))
                {
                    if(existingType == type) return;
                    throw new Exception($"Attempted to map TypeId:{typeId.GuidValue} to: {type.FullName}, but it is already mapped to Type: {existingType.FullName}");
                }

                AssertTypeValidForMapping(type);

                TypeIdToTypeMap.Add(typeId, type);
                TypeToTypeIdMap.Add(type, typeId);
            }

            public void IncludeMappingsFrom(State otherState) => otherState.TypeToTypeIdMap.ForEach(pair => Map(pair.Key, pair.Value));
        }

        static string MapMethodCallforType(Type type) => $@"{nameof(ITypeMappingRegistar.Map)}<{type.GetFullNameCompilable()}>(""{Guid.NewGuid()}"")";
        static string StartOfMappingList => $"endpointBuilder.{nameof(IEndpointBuilder.TypeMapper)}";
    }
}
