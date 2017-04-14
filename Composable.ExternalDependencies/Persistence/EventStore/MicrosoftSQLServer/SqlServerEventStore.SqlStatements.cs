using System;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    static class SqlServerEventStore
    {
        internal static class SqlStatements {
            internal static string FixManualVersionsForAggregate(Guid aggregateId) => $@"
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
    }
}