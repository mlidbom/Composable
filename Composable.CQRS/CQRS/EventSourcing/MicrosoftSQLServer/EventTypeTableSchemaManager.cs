namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    internal class EventTypeTableSchemaManager : TableSchemaManager
    {
        public override string Name { get; } = EventTypeTable.Name;
  

        public override string CreateTableSql => $@"
    CREATE TABLE [dbo].[{EventTypeTable.Name}](
	    [{EventTypeTable.Columns.Id}] [int] IDENTITY(1,1) NOT NULL,
	    [{EventTypeTable.Columns.EventType}] [nvarchar](450) NOT NULL,
        CONSTRAINT [PK_{EventTypeTable.Columns.EventType}] PRIMARY KEY CLUSTERED 
        (
    	    [Id] ASC
        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
        CONSTRAINT [IX_Uniq_{EventTypeTable.Columns.EventType}] UNIQUE
        (
	        {EventTypeTable.Columns.EventType}
        )
    )
";
    }
}