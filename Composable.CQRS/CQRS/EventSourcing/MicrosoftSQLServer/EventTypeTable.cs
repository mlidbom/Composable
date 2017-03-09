namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    static class EventTypeTable
    {
        public static string Name { get; } = "EventType";

        internal static class Columns
        {
            public const string Id = nameof(Id);
            public const string EventType = nameof(EventType);
        }
    }
}