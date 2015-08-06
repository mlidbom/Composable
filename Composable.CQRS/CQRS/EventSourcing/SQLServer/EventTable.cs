namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class EventTable
    {
        public string Name { get; } = "Events";
        public string SelectClause => InternalSelect();

        public string SelectAggregateIdsInCreationOrderSql => $"SELECT {Columns.AggregateId} FROM {Name} WHERE {Columns.AggregateVersion} = 1 ORDER BY {Columns.SqlTimeStamp} ASC";

        public string SelectTopClause(int top) => InternalSelect(top);        

        private string InternalSelect(int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            return $@"SELECT {topClause} {Columns.EventType}, {Columns.Event}, {Columns.AggregateId}, {Columns.AggregateVersion}, {Columns.EventId}, {Columns.TimeStamp} 
FROM {Name} With(UPDLOCK, READCOMMITTED, ROWLOCK) ";
    }

        internal static class Columns
        {
            public const string Id = nameof(Id);
            public const string AggregateId = nameof(AggregateId);
            public const string AggregateVersion = nameof(AggregateVersion);
            public const string TimeStamp = nameof(TimeStamp);
            public const string SqlTimeStamp = nameof(SqlTimeStamp);
            public const string EventType = nameof(EventType);
            public const string EventTypeId = nameof(EventTypeId);
            public const string EventId = nameof(EventId);
            public const string Event = nameof(Event);
        }        
    }
}