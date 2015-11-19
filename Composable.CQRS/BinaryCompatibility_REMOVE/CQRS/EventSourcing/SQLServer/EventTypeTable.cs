using System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal static class EventTypeTable
    {
        public static string Name { get; } = "EventType";
        
        internal static class Columns
        {
            public const string Id = nameof(Id);
            public const string EventType = nameof(EventType);
        }        
    }
}