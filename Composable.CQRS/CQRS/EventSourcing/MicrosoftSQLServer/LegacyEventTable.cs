namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    static class LegacyEventTable
    {
        public static string Name { get; } = "Events";

        internal static class Columns
        {
            public const string AggregateId = nameof(AggregateId);
            public const string AggregateVersion = nameof(AggregateVersion);
            public const string TimeStamp = nameof(TimeStamp);
            public const string EventType = nameof(EventType);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
            public static string SqlTimeStamp = nameof(SqlTimeStamp);
        }        
    }
}