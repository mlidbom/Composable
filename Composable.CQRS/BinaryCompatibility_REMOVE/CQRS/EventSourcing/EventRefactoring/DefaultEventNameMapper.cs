using System;
using Composable.System.Reflection;

namespace Composable.CQRS.EventSourcing.EventRefactoring
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    public class DefaultEventNameMapper : IEventNameMapper
    {
        public string GetName(Type eventType) => eventType.FullName;
        public Type GetType(string eventTypeName) => eventTypeName.AsType();
    }
}