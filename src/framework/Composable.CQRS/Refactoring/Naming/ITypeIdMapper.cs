using System;
using System.Collections.Generic;

namespace Composable.Refactoring.Naming
{
    interface ITypeIdMapper
    {
        TypeId GetId(Type type);
        Type GetType(TypeId eventTypeId);
        void AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings);
    }

    public interface ITypeMappingRegistar
    {
        ITypeMappingRegistar Map<TType>(Guid typeId);
    }

    public static class TypeMappingRegistar
    {
        public static ITypeMappingRegistar Map<TType>(this ITypeMappingRegistar @this, string typeId) => @this.Map<TType>(Guid.Parse(typeId));
    }
}
