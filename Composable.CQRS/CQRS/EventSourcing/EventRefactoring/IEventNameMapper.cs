using System;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    public interface IEventNameMapper
    {
        string GetName(Type eventType);
        Type GetType(string eventTypeName);
    }
}
