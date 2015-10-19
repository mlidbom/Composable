namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal static class EventTable
    {
        public static string Name { get; } = "Event";

        internal static class Columns
        {
            public const string AggregateId = nameof(AggregateId);
            public const string AggregateVersion = nameof(AggregateVersion);
            public const string TimeStamp = nameof(TimeStamp);
            public const string SqlTimeStamp = nameof(SqlTimeStamp);
            public const string InsertionOrder = nameof(InsertionOrder);
            public const string EventType = nameof(EventType);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
        }        
    }
}