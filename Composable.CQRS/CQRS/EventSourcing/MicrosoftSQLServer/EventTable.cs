namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    internal static class EventTable
    {
        public static string Name { get; } = "Event";

        public const string ReadOrderType = "decimal(38,19)";

        internal static class Columns
        {
            public const string AggregateId = nameof(AggregateId);
            public const string InsertedVersion = nameof(InsertedVersion);
            public const string ManualVersion = nameof(ManualVersion);
            public const string EffectiveVersion = nameof(EffectiveVersion);
            public const string InsertionOrder = nameof(InsertionOrder);
            public const string ManualReadOrder = nameof(ManualReadOrder);
            public const string EffectiveReadOrder = nameof(EffectiveReadOrder);
            public const string InsertAfter = nameof(InsertAfter);
            public const string InsertBefore = nameof(InsertBefore);
            public const string Replaces = nameof(Replaces);
            public const string UtcTimeStamp = nameof(UtcTimeStamp);
            public const string SqlInsertTimeStamp = nameof(SqlInsertTimeStamp);
            public const string EventType = nameof(EventType);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
        }        
    }
}