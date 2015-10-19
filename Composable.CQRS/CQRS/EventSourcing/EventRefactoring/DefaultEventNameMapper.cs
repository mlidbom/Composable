using System;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    public class DefaultEventNameMapper : IEventNameMapper
    {
        public string GetName(Type eventType) => eventType.FullName;
        public Type GetType(string eventTypeName) => eventTypeName.AsType();
    }
}