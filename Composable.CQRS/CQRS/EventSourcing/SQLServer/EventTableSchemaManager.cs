namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class EventTableSchemaManager : TableSchemaManager
    {
        override public string Name { get; } = EventTable.Name;

        override public string CreateTableSql => $@"
CREATE TABLE [dbo].[{Name}](
    {EventTable.Columns.InsertionOrder} [bigint] IDENTITY(1,1) NOT NULL,
    {EventTable.Columns.InsertAfter} [bigint] null,
    {EventTable.Columns.InsertBefore} [bigint] null,
    {EventTable.Columns.Replaces} [bigint] null,
    {EventTable.Columns.ReadOrder} decimal(38,19) null,
	{EventTable.Columns.AggregateId} [uniqueidentifier] NOT NULL,
	{EventTable.Columns.AggregateVersion} [int] NOT NULL,
	{EventTable.Columns.TimeStamp} [datetime] NOT NULL,
    {EventTable.Columns.SqlInsertDateTime} [datetime2] default SYSUTCDATETIME(),
    {EventTable.Columns.EventType} [int] NOT NULL,
	{EventTable.Columns.EventId} [uniqueidentifier] NOT NULL,
	{EventTable.Columns.Event} [nvarchar](max) NOT NULL,
	{EventTable.Columns.EffectiveReadOrder} as case 
		when {EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null then cast({EventTable.Columns.InsertionOrder} as decimal(38,19))
		else {EventTable.Columns.ReadOrder}
	end

    CONSTRAINT [IX_Uniq2_{EventTable.Columns.EventId}] UNIQUE
    (
	    {EventTable.Columns.EventId}
    ),

    CONSTRAINT [IX_Uniq_{EventTable.Columns.InsertionOrder}] UNIQUE
    (
	    {EventTable.Columns.InsertionOrder}
    ),
    CONSTRAINT CK_Only_one_reordering_column_specified
    CHECK 
    (
	    ({EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null)
	    or
	    ({EventTable.Columns.InsertAfter} is null and {EventTable.Columns.Replaces} is null)
	    or
	    ({EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null) 
    ),

    CONSTRAINT [PK_{Name}] PRIMARY KEY CLUSTERED 
    (
	    {EventTable.Columns.AggregateId} ASC,
	    {EventTable.Columns.AggregateVersion} ASC
    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF) ON [PRIMARY],

    CONSTRAINT FK_Events_{EventTable.Columns.EventType} FOREIGN KEY ({EventTable.Columns.EventType}) 
        REFERENCES {EventTypeTable.Name} ({EventTypeTable.Columns.Id}),

    CONSTRAINT FK_{EventTable.Columns.Replaces} FOREIGN KEY ( {EventTable.Columns.Replaces} ) 
        REFERENCES Event ({EventTable.Columns.InsertionOrder}),

    CONSTRAINT FK_{EventTable.Columns.InsertBefore} FOREIGN KEY ( {EventTable.Columns.InsertBefore} )
        REFERENCES Event ({EventTable.Columns.InsertionOrder}),

    CONSTRAINT FK_{EventTable.Columns.InsertAfter} FOREIGN KEY ( {EventTable.Columns.InsertAfter} ) 
        REFERENCES Event ({EventTable.Columns.InsertionOrder})
 
) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [{EventTable.Columns.InsertionOrder}] ON [dbo].[{Name}]
(
	[{EventTable.Columns.InsertionOrder}] ASC,
    [{EventTable.Columns.EventType}] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_{EventTable.Columns.Replaces}]	ON [dbo].[{Name}] 
	([{EventTable.Columns.Replaces}])
	INCLUDE ([{EventTable.Columns.InsertionOrder}])

CREATE NONCLUSTERED INDEX [IX_{EventTable.Columns.InsertAfter}]	ON [dbo].[{Name}] 
	([{EventTable.Columns.InsertAfter}])
	INCLUDE ([{EventTable.Columns.InsertionOrder}])

CREATE NONCLUSTERED INDEX [IX_{EventTable.Columns.InsertBefore}]	ON [dbo].[{Name}] 
	([{EventTable.Columns.InsertBefore}])
	INCLUDE ([{EventTable.Columns.InsertionOrder}])

";
    }
}