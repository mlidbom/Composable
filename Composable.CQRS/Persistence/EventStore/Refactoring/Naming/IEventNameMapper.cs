using System;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    interface IEventNameMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeName);
    }
}
