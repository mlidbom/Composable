namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class EventTable : Table
    {
        override public string Name { get; } = "Events";
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

        override public string CreateTableSql => $@"
CREATE TABLE [dbo].[{Name}](
    {Columns.Id} [bigint] IDENTITY(10,10) NOT NULL,
	{Columns.AggregateId} [uniqueidentifier] NOT NULL,
	{Columns.AggregateVersion} [int] NOT NULL,
	{Columns.TimeStamp} [datetime] NOT NULL,
	{Columns.SqlTimeStamp} [timestamp] NOT NULL,
	{Columns.EventType} [varchar](300) NOT NULL,
    {Columns.EventTypeId} int NULL,
	{Columns.EventId} [uniqueidentifier] NOT NULL,
	{Columns.Event} [nvarchar](max) NOT NULL
CONSTRAINT [IX_Uniq_{Columns.EventId}] UNIQUE
(
	{Columns.EventId}
),
CONSTRAINT [IX_Uniq_{Columns.Id}] UNIQUE
(
	{Columns.Id}
),
CONSTRAINT [PK_{Name}] PRIMARY KEY CLUSTERED 
(
	{Columns.AggregateId} ASC,
	{Columns.AggregateVersion} ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY]
) ON [PRIMARY]
CREATE UNIQUE NONCLUSTERED INDEX [{Columns.SqlTimeStamp}] ON [dbo].[{Name}]
(
	[{Columns.SqlTimeStamp}] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
";
    }
}