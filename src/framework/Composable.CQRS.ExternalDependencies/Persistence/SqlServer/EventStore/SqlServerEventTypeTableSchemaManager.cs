namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventTypeTableSchemaManager : SqlServerTableSchemaManager
    {
        internal override string Name { get; } = SqlServerEventTypeTable.Name;

        internal override string CreateTableSql => $@"
CREATE TABLE [dbo].[{SqlServerEventTypeTable.Name}](
	[{SqlServerEventTypeTable.Columns.Id}] [int] IDENTITY(1,1) NOT NULL,
	[{SqlServerEventTypeTable.Columns.EventType}] [UNIQUEIDENTIFIER] NOT NULL,
    CONSTRAINT [PK_{SqlServerEventTypeTable.Columns.EventType}] PRIMARY KEY CLUSTERED 
    (
    	[Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [IX_Uniq_{SqlServerEventTypeTable.Columns.EventType}] UNIQUE
    (
	    {SqlServerEventTypeTable.Columns.EventType}
    )
)";
    }
}