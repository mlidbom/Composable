namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class EventTypeTable : Table
    {
        override public string Name { get; } = "EventType";
        public string SelectClause => InternalSelect();        

        private string InternalSelect(int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            return $@"SELECT {topClause} {Columns.Id}, {Columns.EventType} 
FROM {Name} With(UPDLOCK,READCOMMITTED, ROWLOCK) ";
    }

        internal static class Columns
        {
            public const string Id = nameof(Id);
            public const string EventType = nameof(EventType);
        }

        override public string CreateTableSql => $@"
    CREATE TABLE [dbo].[{Name}](
	[{Columns.Id}] [int] IDENTITY(1,1) NOT NULL,
	[{Columns.EventType}] [varchar](300) NOT NULL,
    CONSTRAINT [PK_{Columns.EventType}] PRIMARY KEY CLUSTERED 
    (
    	[Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
";
    }
}