namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class EventTableSchemaManager : TableSchemaManager
    {
        override public string Name { get; } = EventTable.Name;

        override public string CreateTableSql => $@"
CREATE TABLE dbo.{Name}(
    {EventTable.Columns.InsertionOrder} bigint IDENTITY(1,1) NOT NULL,
    {EventTable.Columns.AggregateId} uniqueidentifier NOT NULL,
    {EventTable.Columns.EffectiveVersion} as case 
        when {EventTable.Columns.ManualVersion} is not null then {EventTable.Columns.ManualVersion}
        when {EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null then {EventTable.Columns.InsertedVersion}
        else null
    end,    
    {EventTable.Columns.TimeStamp} datetime NOT NULL,    
    {EventTable.Columns.EventType} int NOT NULL,    
    {EventTable.Columns.Event} nvarchar(max) NOT NULL,
    {EventTable.Columns.EffectiveReadOrder} as case 
        when {EventTable.Columns.ManualReadOrder} is not null then {EventTable.Columns.ManualReadOrder}
        when {EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null then cast({EventTable.Columns.InsertionOrder} as {EventTable.ReadOrderType})
        else null
    end,
    {EventTable.Columns.EventId} uniqueidentifier NOT NULL,
    {EventTable.Columns.InsertedVersion} int NOT NULL,
    {EventTable.Columns.SqlInsertDateTime} datetime2 default SYSUTCDATETIME(),
    {EventTable.Columns.InsertAfter} bigint null,
    {EventTable.Columns.InsertBefore} bigint null,
    {EventTable.Columns.Replaces} bigint null,
    {EventTable.Columns.ManualReadOrder} {EventTable.ReadOrderType} null,    
    {EventTable.Columns.ManualVersion} int NULL,

    CONSTRAINT PK_{Name} PRIMARY KEY CLUSTERED 
    (
        {EventTable.Columns.AggregateId} ASC,
        {EventTable.Columns.InsertedVersion} ASC
    )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = OFF),

    CONSTRAINT IX_{Name}_Unique_{EventTable.Columns.EventId} UNIQUE
    (
        {EventTable.Columns.EventId}
    ),

    CONSTRAINT IX_{Name}_Unique_{EventTable.Columns.InsertionOrder} UNIQUE
    (
        {EventTable.Columns.InsertionOrder}
    ),

    CONSTRAINT CK_{Name}_Only_one_reordering_column_allowed_for_use
    CHECK 
    (
        ({EventTable.Columns.InsertAfter} is null and {EventTable.Columns.InsertBefore} is null)
        or
        ({EventTable.Columns.InsertAfter} is null and {EventTable.Columns.Replaces} is null)
        or
        ({EventTable.Columns.InsertBefore} is null and {EventTable.Columns.Replaces} is null) 
    ),

    CONSTRAINT FK_{Name}_{EventTable.Columns.EventType} FOREIGN KEY ({EventTable.Columns.EventType}) 
        REFERENCES {EventTypeTable.Name} ({EventTypeTable.Columns.Id}),

    CONSTRAINT FK_{Name}_{EventTable.Columns.Replaces} FOREIGN KEY ( {EventTable.Columns.Replaces} ) 
        REFERENCES {Name} ({EventTable.Columns.InsertionOrder}),

    CONSTRAINT FK_{Name}_{EventTable.Columns.InsertBefore} FOREIGN KEY ( {EventTable.Columns.InsertBefore} )
        REFERENCES {Name} ({EventTable.Columns.InsertionOrder}),

    CONSTRAINT FK_{Name}_{EventTable.Columns.InsertAfter} FOREIGN KEY ( {EventTable.Columns.InsertAfter} ) 
        REFERENCES {Name} ({EventTable.Columns.InsertionOrder}) 
)

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.EffectiveReadOrder} ON dbo.{Name}
        ({EventTable.Columns.EffectiveReadOrder})
        INCLUDE ({EventTable.Columns.EventType}, {EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.Replaces}	ON dbo.{Name}
        ({EventTable.Columns.Replaces})
        INCLUDE ({EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.InsertAfter}	ON dbo.{Name}
        ({EventTable.Columns.InsertAfter})
        INCLUDE ({EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.InsertBefore}	ON dbo.{Name} 
        ({EventTable.Columns.InsertBefore})
        INCLUDE ({EventTable.Columns.InsertionOrder})

    CREATE NONCLUSTERED INDEX IX_{Name}_{EventTable.Columns.EffectiveVersion}	ON dbo.{Name} 
        ({EventTable.Columns.EffectiveVersion})
";


        public string UpdateManualReadOrderValuesSql => $@"
ALTER PROCEDURE CreateReadOrders
AS

set nocount on

declare @{EventTable.Columns.InsertBefore} bigint
declare @{EventTable.Columns.InsertAfter} bigint
declare @{EventTable.Columns.Replaces} bigint
declare @EventsToReorder bigint
declare @BeforeReadOrder {EventTable.ReadOrderType}
declare @AfterReadOrder {EventTable.ReadOrderType}
declare @AvailableSpaceBetwenReadOrders {EventTable.ReadOrderType}
declare @Increment {EventTable.ReadOrderType}
declare @Done bit 
declare @Error nvarchar(4000)
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

            if @AfterReadOrder is null
            begin
                set @Error = 'Failed to find AfterReadOrder during replacement of {EventTable.Columns.InsertionOrder}: ' + cast(@{EventTable.Columns.Replaces} as nvarchar) + ' you are probably trying to replace the last event in the event store. That is not supported.'
                break
            end
           
            set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
            set @Increment = @AvailableSpaceBetwenReadOrders / @EventsToReorder

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
            select @BeforeReadOrder = {EventTable.Columns.EffectiveReadOrder} from {Name} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.InsertAfter}
            if @BeforeReadOrder < 0 --The event we are inserting after has been replaced and it might be by multiple events, so get the highest of the replacing readorders
                select @BeforeReadOrder = max({EventTable.Columns.EffectiveReadOrder}) from {Name} where {EventTable.Columns.Replaces} = @{EventTable.Columns.InsertAfter}

            select top 1 @AfterReadOrder = {EventTable.Columns.EffectiveReadOrder} from {Name} where {EventTable.Columns.EffectiveReadOrder} > @BeforeReadOrder and ({EventTable.Columns.InsertAfter} is null or {EventTable.Columns.InsertAfter} != @{EventTable.Columns.InsertAfter}) order by {EventTable.Columns.EffectiveReadOrder}
            if @AfterReadOrder is null
            begin 
                set @Error = 'Failed to find AfterReadOrder inserting events after {EventTable.Columns.InsertionOrder}: ' + cast(@{EventTable.Columns.InsertAfter} as nvarchar) + ' you are probably trying to insert after the last event in the event store. That is not supported.'
                break
            end

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

            select top 1 @BeforeReadOrder = {EventTable.Columns.EffectiveReadOrder} from {Name} where {EventTable.Columns.EffectiveReadOrder} < @AfterReadOrder and ({EventTable.Columns.InsertBefore} is null or {EventTable.Columns.InsertBefore} != @{EventTable.Columns.InsertBefore}) order by {EventTable.Columns.EffectiveReadOrder} desc
            if(@BeforeReadOrder is null or @BeforeReadOrder < 0)
                set @BeforeReadOrder = cast(0 as {EventTable.ReadOrderType}) --We are inserting before the first event in the whole event store and possibly the original first event has been replaced and thus has a negative {EventTable.Columns.EffectiveReadOrder}

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
    else
    begin 
        set @Done = 1
    end 
end

set nocount off

if @Error is not null 
    raiserror (@Error, 18, -1);
else 
begin

    update {Name} 
    set {EventTable.Columns.ManualVersion} = ChangedReadOrders.NewVersion
    from {Name} 
	    inner join 
    (
	    select * from
	    (select e.{EventTable.Columns.AggregateId}, {EventTable.Columns.InsertedVersion}, row_number() over (partition by e.{EventTable.Columns.AggregateId} order by e.{EventTable.Columns.EffectiveReadOrder}) NewVersion, {EventTable.Columns.EffectiveVersion}
	     from {Name} e
	     inner join (select distinct {EventTable.Columns.AggregateId} from {Name} where {EventTable.Columns.EffectiveVersion} is null) NeedsFixing
		    on e.{EventTable.Columns.AggregateId} = NeedsFixing.{EventTable.Columns.AggregateId}
	     where e.{EventTable.Columns.EffectiveReadOrder} > 0) NewReadOrders
	    where NewReadOrders.{EventTable.Columns.EffectiveVersion} is null or ( NewReadOrders.NewVersion != NewReadOrders.{EventTable.Columns.EffectiveVersion})
    ) ChangedReadOrders

    on {Name}.{EventTable.Columns.AggregateId} = ChangedReadOrders.{EventTable.Columns.AggregateId} and {Name}.{EventTable.Columns.InsertedVersion} = ChangedReadOrders.{EventTable.Columns.InsertedVersion}


    update {Name}
    set {EventTable.Columns.ManualVersion} = -{EventTable.Columns.InsertedVersion}
    where ({EventTable.Columns.EffectiveVersion} > 0 or {EventTable.Columns.EffectiveVersion} is null) and {EventTable.Columns.EffectiveReadOrder} < 0

end 
";
    }
}