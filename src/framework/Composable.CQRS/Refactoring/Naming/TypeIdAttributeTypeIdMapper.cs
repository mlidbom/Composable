using System;

namespace Composable.Refactoring.Naming
{
    class TypeIdAttributeTypeIdMapper : ITypeIdMapper
    {
        public TypeId GetId(Type eventType) => TypeId.FromType(eventType);
        public Type GetType(TypeId typeId) => typeId.GetRuntimeType();
    }
}