using System;

namespace Composable.CQRS.EventSourcing.Refactoring.Naming
{
    public interface IEventNameMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeName);
    }
}
