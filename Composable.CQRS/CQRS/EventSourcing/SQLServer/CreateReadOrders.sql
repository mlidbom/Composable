ALTER PROCEDURE CreateReadOrders
AS

set nocount on

update Event 
set ReadOrder = CAST(InsertionOrder as decimal(38,19))
where InsertBefore is null and InsertAfter is null and Replaces is null and ReadOrder is null


declare @InsertBefore bigint
declare @InsertAfter bigint
declare @Replaces bigint
declare @EventsToReorder bigint
declare @BeforeReadOrder decimal(38,19)
declare @AfterReadOrder decimal(38,19)
declare @AvailableSpaceBetwenReadOrders decimal(38,19)
declare @Increment decimal(38,19)
declare @Done bit 
set @Done = 0


WHILE @Done = 0
begin
	set @InsertAfter = null
	set @InsertBefore = null
	set @Replaces = null
	select top 1 @InsertAfter = InsertAfter,  @InsertBefore = InsertBefore, @Replaces = Replaces
	from Event where ReadOrder is null
	order by InsertionOrder asc

	if @Replaces is not null
		begin 
		   select @EventsToReorder = count(*) from Event where Replaces = @Replaces
		   select @BeforeReadOrder = abs(Readorder) from Event where InsertionOrder = @Replaces


		   select top 1 @AfterReadOrder = Readorder from Event where ReadOrder > @BeforeReadOrder and (Replaces is null or Replaces != @Replaces) order by ReadOrder

		   set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
		   set @Increment = @AvailableSpaceBetwenReadOrders / (@EventsToReorder + 1)

		   update Event set ReadOrder = -ReadOrder where InsertionOrder = @Replaces AND ReadOrder > 0

				update Event
					set ReadOrder = ReadOrders.ReadOrder
				from Event
				inner join 		
					(select InsertionOrder, (@BeforeReadOrder + ((ROW_NUMBER() over (order by InsertionOrder asc)) -1) *  @Increment) as ReadOrder
					from Event
					where Replaces = @Replaces) ReadOrders
					on Event.InsertionOrder = ReadOrders.InsertionOrder
		end 
	else if @InsertAfter is not null
		begin 
		   select @EventsToReorder = count(*) from Event where InsertAfter = @InsertAfter
		   select @BeforeReadOrder = abs(Readorder) from Event where InsertionOrder = @InsertAfter
		   select TOP 1 @AfterReadOrder = Readorder from Event where ReadOrder > @BeforeReadOrder and (InsertAfter is null or InsertAfter != @InsertAfter) order by ReadOrder

		   set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
		   set @Increment = @AvailableSpaceBetwenReadOrders / (@EventsToReorder + 1)


				update Event
					set ReadOrder = ReadOrders.ReadOrder
				from Event
				inner join 		
					(select InsertionOrder, (@BeforeReadOrder + (ROW_NUMBER() over (order by InsertionOrder asc)) *  @Increment) as ReadOrder
					from Event
					where InsertAfter = @InsertAfter) ReadOrders
					on Event.InsertionOrder = ReadOrders.InsertionOrder
		end								
	else if @InsertBefore is not null
		begin 
		   select @EventsToReorder = count(*) from Event where InsertBefore = @InsertBefore
		   
		   select @AfterReadOrder = abs(ReadOrder) from Event where InsertionOrder = @InsertBefore


		   select TOP 1 @BeforeReadOrder = ReadOrder from Event where ReadOrder < @AfterReadOrder and (InsertBefore is null or InsertBefore != @InsertBefore) order by ReadOrder DESC

		   set @AvailableSpaceBetwenReadOrders = @AfterReadOrder - @BeforeReadOrder
		   set @Increment = @AvailableSpaceBetwenReadOrders / (@EventsToReorder + 1)


				update Event
					set ReadOrder = ReadOrders.ReadOrder
				from Event
				inner join 		
					(select InsertionOrder, (@BeforeReadOrder + (ROW_NUMBER() over (order by InsertionOrder asc)) *  @Increment) As ReadOrder
					from Event
					where InsertBefore = @InsertBefore) ReadOrders
					on Event.InsertionOrder = ReadOrders.InsertionOrder
		end 

	if @InsertAfter is null and @InsertBefore is null and @Replaces is null
	begin 
	 set @Done = 1
	end 
end

set nocount off