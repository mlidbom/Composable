using System;

namespace Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer
{
    partial class SqlServerEventStore
    {
        internal class SqlStatements {
            public static string EnsurePersistedMigrationsHaveConsistentReadOrdersAndEffectiveVersionsSqlStoredProcedure => $@"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{nameof(EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions)}]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[{nameof(EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions)}] AS' 
END

go

alter procedure {nameof(EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions)} @ClearExisting bit = 0
as

if @ClearExisting > 0
begin 
	update {EventTable.Name}
	set {EventTable.Columns.ManualReadOrder} = null,
		{EventTable.Columns.ManualVersion} = null
	where {EventTable.Columns.ManualReadOrder} is not null or {EventTable.Columns.ManualVersion} is not null
end 

{EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions}
";
            internal static string FixManualVersionsForAggregate(Guid aggregateId)
            {
                return $@"
update replaced
set replaced.{EventTable.Columns.ManualReadOrder} = -abs(replaced.{EventTable.Columns.EffectiveReadOrder})
from {EventTable.Name} replaced
inner join {EventTable.Name} replaces
	on replaces.{EventTable.Columns.Replaces} = replaced.{EventTable.Columns.InsertionOrder}
where
    replaced.{EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}
and replaces.{EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}
and replaces.{EventTable.Columns.Replaces} is not null
and (replaced.{EventTable.Columns.ManualReadOrder} > 0 or replaced.{EventTable.Columns.ManualReadOrder} is null)

update {EventTable.Name} 
set {EventTable.Columns.ManualVersion} = ChangedReadOrders.NewVersion
from {EventTable.Name} 
	inner join 
(
	select * from
	(select e.{EventTable.Columns.AggregateId}, {EventTable.Columns.InsertedVersion}, row_number() over (partition by e.{EventTable.Columns.AggregateId} order by e.{EventTable.Columns.EffectiveReadOrder}) NewVersion, {EventTable.Columns.EffectiveVersion}
	    from {EventTable.Name} e
	    where e.{EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}
            and e.{EventTable.Columns.EffectiveReadOrder} > 0
        ) NewReadOrders
	where NewReadOrders.{EventTable.Columns.EffectiveVersion} is null or ( NewReadOrders.NewVersion != NewReadOrders.{EventTable.Columns.EffectiveVersion})
) ChangedReadOrders

on {EventTable.Name}.{EventTable.Columns.AggregateId} = ChangedReadOrders.{EventTable.Columns.AggregateId} and {EventTable.Name}.{EventTable.Columns.InsertedVersion} = ChangedReadOrders.{EventTable.Columns.InsertedVersion}


update {EventTable.Name}
set {EventTable.Columns.ManualVersion} = -{EventTable.Columns.InsertedVersion}
where {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}
    and ({EventTable.Columns.EffectiveVersion} > 0 or {EventTable.Columns.EffectiveVersion} is null) 
    and {EventTable.Columns.EffectiveReadOrder} < 0
";
            }

            static string EnsurePersistedMigrationsHaveConsistentEffectiveReadOrdersAndEffectiveVersions => $@"
set nocount on

declare @{EventTable.Columns.InsertionOrder} bigint
declare @{EventTable.Columns.InsertBefore} bigint
declare @{EventTable.Columns.InsertAfter} bigint
declare @{EventTable.Columns.Replaces} bigint
declare @NumberOfEventsToReorder bigint
declare @BeforeReadOrder {EventTable.ReadOrderType}
declare @AfterReadOrder {EventTable.ReadOrderType}
declare @AvailableSpaceBetwenReadOrders {EventTable.ReadOrderType}
declare @Increment {EventTable.ReadOrderType}
declare @Done bit 
declare @Error nvarchar(4000)
set @Done = 0

WHILE @Done = 0
begin
/* Uncomment this to debug 
    select * from {EventTable.Name}
    order by {EventTable.Columns.EffectiveReadOrder}
*/

    set @{EventTable.Columns.InsertionOrder} = null
    set @{EventTable.Columns.InsertAfter} = null
    set @{EventTable.Columns.InsertBefore} = null
    set @{EventTable.Columns.Replaces} = null
    select top 1 @{EventTable.Columns.InsertAfter} = {EventTable.Columns.InsertAfter},  @{EventTable.Columns.InsertBefore} = {EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces} = {EventTable.Columns.Replaces}, @{EventTable.Columns.InsertionOrder} = {EventTable.Columns.InsertionOrder}
    from {EventTable.Name} where {EventTable.Columns.EffectiveReadOrder} is null
    order by {EventTable.Columns.InsertionOrder} asc

    if @{EventTable.Columns.Replaces} is not null
        begin 
            set @NumberOfEventsToReorder = (select count(*) from {EventTable.Name} where {EventTable.Columns.Replaces} = @{EventTable.Columns.Replaces})
            set @BeforeReadOrder = (select abs({EventTable.Columns.EffectiveReadOrder}) from {EventTable.Name} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.Replaces})
            set @AfterReadOrder = (select top 1 {EventTable.Columns.EffectiveReadOrder} from {EventTable.Name} where {EventTable.Columns.EffectiveReadOrder} > @BeforeReadOrder and ({EventTable.Columns.Replaces} is null or {EventTable.Columns.Replaces} != @{EventTable.Columns.Replaces}) order by {EventTable.Columns.EffectiveReadOrder})

            if @AfterReadOrder is null
            begin
                if (select max({EventTable.Columns.InsertionOrder}) from {EventTable.Name} where {EventTable.Columns.Replaces} = @{EventTable.Columns.Replaces}) = (select max({EventTable.Columns.InsertionOrder}) from {EventTable.Name}) --There are no other events in the whole store after the replaced event, except for the replacing events.
                begin 
                    set @AfterReadOrder = (select max({EventTable.Columns.InsertionOrder}) from {EventTable.Name} where {EventTable.Columns.Replaces} = @{EventTable.Columns.Replaces})
                end
				else if @BeforeReadOrder = (select max({EventTable.Columns.EffectiveReadOrder}) from {EventTable.Name}) --There are no other events in the whole store after the event we are replacing
                begin 
                    set @AfterReadOrder = cast((@BeforeReadOrder + 1) as int) --Future events will be inserted at an index no lower than the next integer
                end
                else
                begin
                    set @Error = 'Failed to find AfterReadOrder during replacement of {EventTable.Columns.InsertionOrder}: ' + cast(@{EventTable.Columns.Replaces} as nvarchar)
                    break
                end
            end
           
            set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
            set @Increment = @AvailableSpaceBetwenReadOrders / @NumberOfEventsToReorder

            update {EventTable.Name} set ManualReadOrder = -{EventTable.Columns.EffectiveReadOrder} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.Replaces} AND {EventTable.Columns.EffectiveReadOrder} > 0

            update {EventTable.Name}
                set ManualReadOrder = ReadOrders.{EventTable.Columns.EffectiveReadOrder}
            from {EventTable.Name}
                inner join 		
                    (select {EventTable.Columns.InsertionOrder}, (@BeforeReadOrder + ((ROW_NUMBER() over (order by {EventTable.Columns.InsertionOrder} asc)) -1) *  @Increment) as {EventTable.Columns.EffectiveReadOrder}
                    from {EventTable.Name}
                    where {EventTable.Columns.Replaces} = @{EventTable.Columns.Replaces}) ReadOrders
                on {EventTable.Name}.{EventTable.Columns.InsertionOrder} = ReadOrders.{EventTable.Columns.InsertionOrder}
        end 
    else if @{EventTable.Columns.InsertAfter} is not null
        begin 
            set @NumberOfEventsToReorder = (select count(*) from {EventTable.Name} where {EventTable.Columns.InsertAfter} = @{EventTable.Columns.InsertAfter})
            set @BeforeReadOrder = (select {EventTable.Columns.EffectiveReadOrder} from {EventTable.Name} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.InsertAfter})
            if @BeforeReadOrder < 0 --The event we are inserting after has been replaced and it might be by multiple events, so get the highest of the replacing readorders
                set @BeforeReadOrder = (select max({EventTable.Columns.EffectiveReadOrder}) from {EventTable.Name} where {EventTable.Columns.Replaces} = @{EventTable.Columns.InsertAfter})

            set @AfterReadOrder = (select top 1 {EventTable.Columns.EffectiveReadOrder} from {EventTable.Name} where {EventTable.Columns.EffectiveReadOrder} > @BeforeReadOrder and ({EventTable.Columns.InsertAfter} is null or {EventTable.Columns.InsertAfter} != @{EventTable.Columns.InsertAfter}) order by {EventTable.Columns.EffectiveReadOrder})
            if @AfterReadOrder is null
            begin
                if (select max({EventTable.Columns.InsertionOrder}) from {EventTable.Name} where {EventTable.Columns.InsertAfter} = @{EventTable.Columns.InsertAfter}) = (select max({EventTable.Columns.InsertionOrder}) from {EventTable.Name}) --There are no other events in the whole store after the event we are inserting after.
                begin 
                    set @AfterReadOrder = (select max({EventTable.Columns.InsertionOrder}) from {EventTable.Name} where {EventTable.Columns.InsertAfter} = @{EventTable.Columns.InsertAfter})
                end
				else if @BeforeReadOrder = (select max({EventTable.Columns.EffectiveReadOrder}) from {EventTable.Name}) --There are no other events in the whole store after the event we are inserting after
                begin 
                    set @AfterReadOrder = cast((@BeforeReadOrder + 1) as int) --Future events will be inserted at an effective readorder no lower than the next integer
                end 
                else
                begin
                    set @Error = 'Failed to find AfterReadOrder while inserting after {EventTable.Columns.InsertionOrder}: ' + cast(@{EventTable.Columns.InsertAfter} as nvarchar)
                    break
                end
            end

            set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
            set @Increment = @AvailableSpaceBetwenReadOrders / (@NumberOfEventsToReorder + 1)

            update {EventTable.Name}
                set ManualReadOrder = ReadOrders.{EventTable.Columns.EffectiveReadOrder}
            from {EventTable.Name}
                inner join 		
                    (select {EventTable.Columns.InsertionOrder}, (@BeforeReadOrder + (ROW_NUMBER() over (order by {EventTable.Columns.InsertionOrder} asc)) *  @Increment) as {EventTable.Columns.EffectiveReadOrder}
                    from {EventTable.Name}
                    where {EventTable.Columns.InsertAfter} = @{EventTable.Columns.InsertAfter}) ReadOrders
                on {EventTable.Name}.{EventTable.Columns.InsertionOrder} = ReadOrders.{EventTable.Columns.InsertionOrder}
        end								
    else if @{EventTable.Columns.InsertBefore} is not null
        begin 
            select @NumberOfEventsToReorder = count(*) from {EventTable.Name} where InsertBefore = @{EventTable.Columns.InsertBefore}
		   
            select @AfterReadOrder = abs({EventTable.Columns.EffectiveReadOrder}) from {EventTable.Name} where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.InsertBefore}

            select top 1 @BeforeReadOrder = {EventTable.Columns.EffectiveReadOrder} from {EventTable.Name} where {EventTable.Columns.EffectiveReadOrder} < @AfterReadOrder and ({EventTable.Columns.InsertBefore} is null or {EventTable.Columns.InsertBefore} != @{EventTable.Columns.InsertBefore}) order by {EventTable.Columns.EffectiveReadOrder} desc
            if(@BeforeReadOrder is null or @BeforeReadOrder < 0)
                set @BeforeReadOrder = cast(0 as {EventTable.ReadOrderType}) --We are inserting before the first event in the whole event store and possibly the original first event has been replaced and thus has a negative {EventTable.Columns.EffectiveReadOrder}

            set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
            set @Increment = @AvailableSpaceBetwenReadOrders / (@NumberOfEventsToReorder + 1)


            update {EventTable.Name}
                set ManualReadOrder = ReadOrders.{EventTable.Columns.EffectiveReadOrder}
            from {EventTable.Name}
                inner join 		
                    (select {EventTable.Columns.InsertionOrder}, (@BeforeReadOrder + (ROW_NUMBER() over (order by {EventTable.Columns.InsertionOrder} asc)) *  @Increment) As {EventTable.Columns.EffectiveReadOrder}
                    from {EventTable.Name}
                    where {EventTable.Columns.InsertBefore} = @{EventTable.Columns.InsertBefore}) ReadOrders
                on {EventTable.Name}.{EventTable.Columns.InsertionOrder} = ReadOrders.{EventTable.Columns.InsertionOrder}
        end
    else
    begin 
        set @Done = 1
    end 
end

if @Error is not null 
    raiserror (@Error, 18, -1);
else 
begin

    update {EventTable.Name} 
    set {EventTable.Columns.ManualVersion} = ChangedReadOrders.NewVersion
    from {EventTable.Name} 
	    inner join 
    (
	    select * from
	    (select e.{EventTable.Columns.AggregateId}, {EventTable.Columns.InsertedVersion}, row_number() over (partition by e.{EventTable.Columns.AggregateId} order by e.{EventTable.Columns.EffectiveReadOrder}) NewVersion, {EventTable.Columns.EffectiveVersion}
	        from {EventTable.Name} e
	        inner join (select distinct {EventTable.Columns.AggregateId} from {EventTable.Name} where {EventTable.Columns.EffectiveVersion} is null) NeedsFixing
		    on e.{EventTable.Columns.AggregateId} = NeedsFixing.{EventTable.Columns.AggregateId}
	        where e.{EventTable.Columns.EffectiveReadOrder} > 0) NewReadOrders
	    where NewReadOrders.{EventTable.Columns.EffectiveVersion} is null or ( NewReadOrders.NewVersion != NewReadOrders.{EventTable.Columns.EffectiveVersion})
    ) ChangedReadOrders

    on {EventTable.Name}.{EventTable.Columns.AggregateId} = ChangedReadOrders.{EventTable.Columns.AggregateId} and {EventTable.Name}.{EventTable.Columns.InsertedVersion} = ChangedReadOrders.{EventTable.Columns.InsertedVersion}


    update {EventTable.Name}
    set {EventTable.Columns.ManualVersion} = -{EventTable.Columns.InsertedVersion}
    where ({EventTable.Columns.EffectiveVersion} > 0 or {EventTable.Columns.EffectiveVersion} is null) and {EventTable.Columns.EffectiveReadOrder} < 0

end 

set nocount off";
        }
    }
}