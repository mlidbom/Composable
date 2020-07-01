namespace Composable.Persistence.SqlServer.EventStore
{
    static class SqlServerEventStore
    {
        internal static class SqlStatements {
            internal static readonly string FixManualVersionsForAggregate = $@"
update replaced
set replaced.{SqlServerEventTable.Columns.EffectiveOrder} = -abs(replaced.{SqlServerEventTable.Columns.EffectiveOrder})
from {SqlServerEventTable.Name} replaced
inner join {SqlServerEventTable.Name} replaces
	on replaces.{SqlServerEventTable.Columns.Replaces} = replaced.{SqlServerEventTable.Columns.EventId}
where
    replaced.{SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}
and replaces.{SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}
and replaces.{SqlServerEventTable.Columns.Replaces} is not null
and (replaced.{SqlServerEventTable.Columns.EffectiveOrder} > 0 or replaced.{SqlServerEventTable.Columns.EffectiveOrder} is null)

update {SqlServerEventTable.Name} 
set {SqlServerEventTable.Columns.EffectiveVersion} = ChangedReadOrders.NewVersion
from {SqlServerEventTable.Name} 
	inner join 
(
	select * from
	(select e.{SqlServerEventTable.Columns.AggregateId}, {SqlServerEventTable.Columns.InsertedVersion}, row_number() over (partition by e.{SqlServerEventTable.Columns.AggregateId} order by e.{SqlServerEventTable.Columns.EffectiveOrder}) NewVersion, {SqlServerEventTable.Columns.EffectiveVersion}
	    from {SqlServerEventTable.Name} e
	    where e.{SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}
            and e.{SqlServerEventTable.Columns.EffectiveOrder} > 0
        ) NewReadOrders
	where NewReadOrders.{SqlServerEventTable.Columns.EffectiveVersion} is null or ( NewReadOrders.NewVersion != NewReadOrders.{SqlServerEventTable.Columns.EffectiveVersion})
) ChangedReadOrders

on {SqlServerEventTable.Name}.{SqlServerEventTable.Columns.AggregateId} = ChangedReadOrders.{SqlServerEventTable.Columns.AggregateId} and {SqlServerEventTable.Name}.{SqlServerEventTable.Columns.InsertedVersion} = ChangedReadOrders.{SqlServerEventTable.Columns.InsertedVersion}


update {SqlServerEventTable.Name}
set {SqlServerEventTable.Columns.EffectiveVersion} = -{SqlServerEventTable.Columns.InsertedVersion}
where {SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}
    and ({SqlServerEventTable.Columns.EffectiveVersion} > 0 or {SqlServerEventTable.Columns.EffectiveVersion} is null) 
    and {SqlServerEventTable.Columns.EffectiveOrder} < 0
";
        }
    }
}