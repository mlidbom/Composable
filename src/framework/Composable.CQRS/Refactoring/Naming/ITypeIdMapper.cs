using System;

namespace Composable.Refactoring.Naming
{
    interface ITypeIdMapper
    {
        TypeId GetId(Type eventType);
        Type GetType(TypeId eventTypeId);
    }
}
