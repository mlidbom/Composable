using System;

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Naming
{
    interface IEventNameMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeName);
    }
}
