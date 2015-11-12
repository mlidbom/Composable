using System;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Naming
{
    public interface IEventNameMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeName);
    }
}
