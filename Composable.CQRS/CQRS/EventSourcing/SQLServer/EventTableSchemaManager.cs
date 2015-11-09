namespace Composable.CQRS.EventSourcing.SQLServer
{
    public partial class EventTableSchemaManager : TableSchemaManager
    {
        override public string Name { get; } = EventTable.Name;

        override public string CreateTableSql => $@"
CREATE TABLE [dbo].[{Name}](
    {EventTable.Columns.InsertionOrder} [bigint] IDENTITY(1,1) NOT NULL,
    {EventTable.Columns.InsertAfter} [bigint] null,
    {EventTable.Columns.InsertBefore} [bigint] null,
    {EventTable.Columns.Replaces} [bigint] null,
    {EventTable.Columns.ManualReadOrder} decimal(38,19) null,
	{EventTable.Columns.AggregateId} [uniqueidentifier] NOT NULL,
	{EventTable.Columns.AggregateVersion} [int] NOT NULL,
	{EventTable.Columns.TimeStamp} [datetime] NOT NULL,
    {EventTable.Columns.SqlInsertDateTime} [datetime2] default SYSUTCDATETIME(),
    {EventTable.Columns.EventType} [int] NOT NULL,
	{EventTable.Columns.EventId} [uniqueidentifier] NOT NULL,
	{EventTable.Columns.Event} [nvarchar](max) NOT NULL,
	{EventTable.Columns.EffectiveReadOrder} as case 
		when {EventTable.Columns.ManualReadOrder} is not null then {EventTable.Columns.ManualReadOrder}
        when {EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null then cast({EventTable.Columns.InsertionOrder} as decimal(38,19))
		else null
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

        public string CreateManualOrderEntriesSql => $@"
ALTER PROCEDURE CreateReadOrders
AS

set nocount on

declare @{EventTable.Columns.InsertBefore} bigint
declare @{EventTable.Columns.InsertAfter} bigint
declare @{EventTable.Columns.Replaces} bigint
declare @EventsToReorder bigint
declare @BeforeReadOrder decimal(38,19)
declare @AfterReadOrder decimal(38,19)
declare @AvailableSpaceBetwenReadOrders decimal(38,19)
declare @Increment decimal(38,19)
declare @Done bit 
set @Done = 0


WHILE @Done = 0
begin
	set @{EventTable.Columns.InsertAfter} = null
	set @{EventTable.Columns.InsertBefore} = null
	set @{EventTable.Columns.Replaces} = null
	select top 1 @{EventTable.Columns.InsertAfter} = {EventTable.Columns.InsertAfter},  @{EventTable.Columns.InsertBefore} = {EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces} = {EventTable.Columns.Replaces}
	from {Name} where {EventTable.Columns.EffectiveReadOrder} is null
	order by {EventTable.Columns.InsertionOrder} asc

	if @{EventTable.Columns.Replaces} is not null
		begin 
		   select @EventsToReorder = count(*) from {Name} where {EventTable.Columns.Replaces} = @{EventTable.Columns.Replaces}
		   select @BeforeReadOrder = abs({EventTable.Columns.EffectiveReadOrder}) from {Name} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.Replaces}


		   select top 1 @AfterReadOrder = {EventTable.Columns.EffectiveReadOrder} from {Name} where {EventTable.Columns.EffectiveReadOrder} > @BeforeReadOrder and ({EventTable.Columns.Replaces} is null or {EventTable.Columns.Replaces} != @{EventTable.Columns.Replaces}) order by {EventTable.Columns.EffectiveReadOrder}

		   set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
		   set @Increment = @AvailableSpaceBetwenReadOrders / (@EventsToReorder + 1)

		   update {Name} set ManualReadOrder = -{EventTable.Columns.EffectiveReadOrder} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.Replaces} AND {EventTable.Columns.EffectiveReadOrder} > 0

			update {Name}
				set ManualReadOrder = ReadOrders.{EventTable.Columns.EffectiveReadOrder}
			from {Name}
			inner join 		
				(select {EventTable.Columns.InsertionOrder}, (@BeforeReadOrder + ((ROW_NUMBER() over (order by {EventTable.Columns.InsertionOrder} asc)) -1) *  @Increment) as {EventTable.Columns.EffectiveReadOrder}
				from {Name}
				where {EventTable.Columns.Replaces} = @{EventTable.Columns.Replaces}) ReadOrders
				on {Name}.{EventTable.Columns.InsertionOrder} = ReadOrders.{EventTable.Columns.InsertionOrder}
		end 
	else if @{EventTable.Columns.InsertAfter} is not null
		begin 
		   select @EventsToReorder = count(*) from {Name} where {EventTable.Columns.InsertAfter} = @{EventTable.Columns.InsertAfter}
		   select @BeforeReadOrder = abs({EventTable.Columns.EffectiveReadOrder}) from {Name} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.InsertAfter}
		   select TOP 1 @AfterReadOrder = {EventTable.Columns.EffectiveReadOrder} from {Name} where {EventTable.Columns.EffectiveReadOrder} > @BeforeReadOrder and ({EventTable.Columns.InsertAfter} is null or {EventTable.Columns.InsertAfter} != @{EventTable.Columns.InsertAfter}) order by {EventTable.Columns.EffectiveReadOrder}

		   set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
		   set @Increment = @AvailableSpaceBetwenReadOrders / (@EventsToReorder + 1)

			update {Name}
				set ManualReadOrder = ReadOrders.{EventTable.Columns.EffectiveReadOrder}
			from {Name}
			inner join 		
				(select {EventTable.Columns.InsertionOrder}, (@BeforeReadOrder + (ROW_NUMBER() over (order by {EventTable.Columns.InsertionOrder} asc)) *  @Increment) as {EventTable.Columns.EffectiveReadOrder}
				from {Name}
				where {EventTable.Columns.InsertAfter} = @{EventTable.Columns.InsertAfter}) ReadOrders
				on {Name}.{EventTable.Columns.InsertionOrder} = ReadOrders.{EventTable.Columns.InsertionOrder}
		end								
	else if @{EventTable.Columns.InsertBefore} is not null
		begin 
		   select @EventsToReorder = count(*) from {Name} where InsertBefore = @{EventTable.Columns.InsertBefore}
		   
		   select @AfterReadOrder = abs({EventTable.Columns.EffectiveReadOrder}) from {Name} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.InsertBefore}


		   select TOP 1 @BeforeReadOrder = {EventTable.Columns.EffectiveReadOrder} from {Name} where {EventTable.Columns.EffectiveReadOrder} < @AfterReadOrder and ({EventTable.Columns.InsertBefore} is null or {EventTable.Columns.InsertBefore} != @{EventTable.Columns.InsertBefore}) order by {EventTable.Columns.EffectiveReadOrder} DESC
		   if(@BeforeReadOrder is null or @BeforeReadOrder < 0)
				set @BeforeReadOrder = cast(0 as decimal(38,19)) --We are inserting before the first event in the whole event store and possibly the original first event has been replace and thus has a negative {EventTable.Columns.EffectiveReadOrder}

		   set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
		   set @Increment = @AvailableSpaceBetwenReadOrders / (@EventsToReorder + 1)


			update {Name}
				set ManualReadOrder = ReadOrders.{EventTable.Columns.EffectiveReadOrder}
			from {Name}
			inner join 		
				(select {EventTable.Columns.InsertionOrder}, (@BeforeReadOrder + (ROW_NUMBER() over (order by {EventTable.Columns.InsertionOrder} asc)) *  @Increment) As {EventTable.Columns.EffectiveReadOrder}
				from {Name}
				where {EventTable.Columns.InsertBefore} = @{EventTable.Columns.InsertBefore}) ReadOrders
				on {Name}.{EventTable.Columns.InsertionOrder} = ReadOrders.{EventTable.Columns.InsertionOrder}
		end

	if @{EventTable.Columns.InsertAfter} is null and @{EventTable.Columns.InsertBefore} is null and @{EventTable.Columns.Replaces} is null
	begin 
	 set @Done = 1
	end 
end

set nocount off
";
    }
}