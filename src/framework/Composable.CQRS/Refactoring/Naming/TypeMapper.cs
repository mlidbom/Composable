using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Refactoring.Naming
{
    class TypeMapper : ITypeMapper, ITypeMappingRegistar
    {
        readonly IThreadShared<State> _state = ThreadShared<State>.Optimized();

        public TypeId GetId(Type type) => _state.WithExclusiveAccess(state =>
        {
            if(state.TypeToTypeIdMap.TryGetValue(type, out var typeId))
            {
                return typeId;
            }

            throw BuildExceptionDescribingHowToAddMissingMappings(new List<Type> {type});
        });

        public Type GetType(TypeId typeId) => _state.WithExclusiveAccess(state =>
        {
            if(state.TypeIdToTypeMap.TryGetValue(typeId, out var type))
            {
                return type;
            }

            throw new Exception($"Could not find type for {nameof(TypeId)}: {typeId}");
        });

        public bool TryGetType(TypeId typeId, out Type type)
        {
            type = _state.WithExclusiveAccess(state => state.TypeIdToTypeMap.TryGetValue(typeId, out var innerType) ? innerType : null);

            return type != null;
        }

        public void AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings) => _state.WithExclusiveAccess(state =>
        {
            var typesWithMissingMappings = typesThatRequireMappings.Where(type => !state.TypeToTypeIdMap.ContainsKey(type)).ToList();
            if(typesWithMissingMappings.Any())
            {
                throw BuildExceptionDescribingHowToAddMissingMappings(typesWithMissingMappings);
            }
        });


        public void MergeMappingsWith(TypeMapper other) => _state.WithExclusiveAccess(state => other._state.WithExclusiveAccess(otherState =>
        {
            var originalOtherMap = otherState.TypeToTypeIdMap.ToList();
            state.TypeToTypeIdMap.ForEach(pair => other.InternalMap(pair.Key, pair.Value));
            originalOtherMap.ForEach(pair => InternalMap(pair.Key, pair.Value));
        }));

        public ITypeMappingRegistar Map<TType>(Guid typeIdGuid) => InternalMap(typeof(TType), new TypeId(typeIdGuid));
        public ITypeMappingRegistar Map<TType>(string typeGuid) => Map<TType>(Guid.Parse(typeGuid));

        ITypeMappingRegistar InternalMap(Type type, TypeId typeId) => _state.WithExclusiveAccess(state =>
        {
            if(state.TypeToTypeIdMap.TryGetValue(type, out var existingTypeId))
            {
                if(existingTypeId == typeId) return this;
                throw new Exception($"Attempted to map Type:{type.FullName} to: {typeId}, but it is already mapped to: {existingTypeId}");
            }

            AssertTypeValidForMapping(type);

            state.TypeIdToTypeMap.Add(typeId, type);
            state.TypeToTypeIdMap.Add(type, typeId);

            return this;
        });

        static void AssertTypeValidForMapping(Type type)
        {
            if(type.IsAbstract)
            {
                if(!(typeof(BusApi.Remotable.IEvent).IsAssignableFrom(type)))
                {
                    throw new Exception($"Type: {type.FullName} is abstract and is not a {typeof(BusApi.Remotable.IEvent).FullName}. For other types you should only map concrete types.");
                }
            }
        }

        Exception BuildExceptionDescribingHowToAddMissingMappings(List<Type> typesWithMissingMappings)
        {
            typesWithMissingMappings = typesWithMissingMappings.Distinct().OrderBy(type => type.GetFullNameCompilable()).ToList();

            var fixMessage = new StringBuilder();
            fixMessage.AppendLine($@"
In order to allow you to freely rename and move your types without breaking your persisted data you are required to map your types to Guid values that are used in place of your type names in the persisted data.
Some such required type mappings are missing. 
You should map them in your endpoint configuration by using {typeof(IEndpointBuilder)}.{nameof(IEndpointBuilder.TypeMapper)}")
                      .Append(StartOfMappingList);

            typesWithMissingMappings.ForEach(type => fixMessage.Append($"{Environment.NewLine}   .{MapMethodCallforType(type)}"));

            fixMessage.Append(";").AppendLine().AppendLine();

            return new Exception(fixMessage.ToString());
        }

        class State
        {
            public readonly Dictionary<Type, TypeId> TypeToTypeIdMap = new Dictionary<Type, TypeId>();
            public readonly Dictionary<TypeId, Type> TypeIdToTypeMap = new Dictionary<TypeId, Type>();
        }

        string MapMethodCallforType(Type type) => $@"{nameof(ITypeMappingRegistar.Map)}<{type.GetFullNameCompilable()}>(""{Guid.NewGuid()}"")";
        string StartOfMappingList => $"endpointBuilder.{nameof(IEndpointBuilder.TypeMapper)}";
    }
}
