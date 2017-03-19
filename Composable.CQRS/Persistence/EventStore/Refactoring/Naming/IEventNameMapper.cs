using System;

namespace Composable.Persistence.EventStore.Refactoring.Naming
{
    interface IEventNameMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeName);
    }
}
