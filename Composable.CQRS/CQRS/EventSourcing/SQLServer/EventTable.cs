namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal static class EventTable
    {
        public static string Name { get; } = "Event";

        internal static class Columns
        {
            public const string AggregateId = nameof(AggregateId);
            public const string AggregateVersion = nameof(AggregateVersion);
            public const string InsertionOrder = nameof(InsertionOrder);
            public const string InsertAfter = nameof(InsertAfter);
            public const string InsertBefore = nameof(InsertBefore);
            public const string Replaces = nameof(Replaces);
            public const string TimeStamp = nameof(TimeStamp);
            public const string SqlInsertDateTime = nameof(SqlInsertDateTime);
            public const string EventType = nameof(EventType);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
            public const string ReadOrder = nameof(ReadOrder);
            public const string EffectiveReadOrder = nameof(EffectiveReadOrder);
        }        
    }
}