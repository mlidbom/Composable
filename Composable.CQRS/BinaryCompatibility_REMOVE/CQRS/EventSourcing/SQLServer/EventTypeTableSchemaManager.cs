using System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal class EventTypeTableSchemaManager : TableSchemaManager
    {
        override public string Name { get; } = EventTypeTable.Name;
  

        override public string CreateTableSql => $@"
    CREATE TABLE [dbo].[{EventTypeTable.Name}](
	[{EventTypeTable.Columns.Id}] [int] IDENTITY(1,1) NOT NULL,
	[{EventTypeTable.Columns.EventType}] [varchar](300) NOT NULL,
    CONSTRAINT [PK_{EventTypeTable.Columns.EventType}] PRIMARY KEY CLUSTERED 
    (
    	[Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [IX_Uniq_{EventTypeTable.Columns.EventType}] UNIQUE
    (
	    {EventTypeTable.Columns.EventType}
    )
    ) ON [PRIMARY]
";
    }
}