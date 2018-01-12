using System;

namespace Composable.Persistence.EventStore.Refactoring.Naming
{
    interface ITypeIdMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeId);
    }
}
