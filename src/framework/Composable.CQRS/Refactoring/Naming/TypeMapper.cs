using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Composable.Messaging.Buses;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Refactoring.Naming
{
    class TypeMapper : ITypeIdMapper, ITypeMappingRegistar
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

            return typeId.GetRuntimeType();
        });

        public void AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings) => _state.WithExclusiveAccess(state =>
        {
            var typesWithMissingMappings = typesThatRequireMappings.Where(type => !state.TypeToTypeIdMap.ContainsKey(type)).ToList();
            if(typesWithMissingMappings.Any())
            {
                BuildExceptionDescribingHowToAddMissingMappings(typesWithMissingMappings);
            }
        });

        public ITypeMappingRegistar Map<TType>(Guid typeIdGuid) => _state.WithExclusiveAccess(state =>
        {
            if(state.TypeToTypeIdMap.TryGetValue(typeof(TType), out var existingTypeId))
            {
                throw new Exception($"Type:{typeof(TType).FullName} is already mapped to: {existingTypeId}");
            }

            var typeId = new TypeId(typeIdGuid, Guid.Empty);
            state.TypeIdToTypeMap.Add(typeId, typeof(TType));
            state.TypeToTypeIdMap.Add(typeof(TType), typeId);

            return this;
        });

        Exception BuildExceptionDescribingHowToAddMissingMappings(List<Type> typesWithMissingMappings)
        {
            typesWithMissingMappings = typesWithMissingMappings.Distinct().OrderBy(type => type.FullName).ToList();

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

        string MapMethodCallforType(Type type) => $@"{nameof(ITypeMappingRegistar.Map)}<{type.FullName.Replace("+", ".")}>(""{Guid.NewGuid()}"")";
        string StartOfMappingList => $"endpointBuilder.{nameof(IEndpointBuilder.TypeMapper)}";
    }
}
