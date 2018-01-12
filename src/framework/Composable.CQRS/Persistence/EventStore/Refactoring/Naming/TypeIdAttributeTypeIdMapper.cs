using System;

namespace Composable.Persistence.EventStore.Refactoring.Naming
{
    class TypeIdAttributeTypeIdMapper : ITypeIdMapper
    {
        public string GetName(Type eventType) => TypeId.FromType(eventType).ToString();
        public Type GetType(string eventTypeId) => TypeId.Parse(eventTypeId).GetRuntimeType();
    }
}