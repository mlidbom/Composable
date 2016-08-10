using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System;
using Composable.System.Linq;
using Newtonsoft.Json;

namespace Composable.CQRS.EventSourcing.MicrosoftSQLServer
{
    internal class  SqlServerEventStoreEventWriter
    {
        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        private readonly SqlServerEvestStoreEventSerializer _eventSerializer;
        private IEventTypeToIdMapper IdMapper => _schemaManager.IdMapper;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;

        public SqlServerEventStoreEventWriter(SqlServerEventStoreConnectionManager connectionMananger, SqlServerEvestStoreEventSerializer eventSerializer, SqlServerEventStoreSchemaManager schemaManager)
        {
            _connectionMananger = connectionMananger;
            _eventSerializer = eventSerializer;
            _schemaManager = schemaManager;
        }

        //Review:catch primary key violation errors and rethrow in an optimistic concurrency failure exception.: 
        public void Insert(IEnumerable<AggregateRootEvent> events)
        {
            SaveEventsInternal(events.Select(@this => new EventWithManualReadorder() {Event = @this, ManualReadOrder = SqlDecimal.Null}));
        }

        private void SaveEventsInternal(IEnumerable<EventWithManualReadorder> events)
        {
            using (var connection = _connectionMananger.OpenConnection())
            {
                foreach (var refactoringEvent in events)
                {
                    var @event = refactoringEvent.Event;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;

                        command.CommandText +=
                            $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.InsertedVersion},  {EventTable.Columns.ManualVersion}, {EventTable.Columns.ManualReadOrder}, {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.UtcTimeStamp},  {EventTable.Columns.Event},  {EventTable.Columns.InsertAfter}, {EventTable.Columns.InsertBefore},  {EventTable.Columns.Replaces}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.InsertedVersion}, @{EventTable.Columns.ManualVersion}, @{EventTable.Columns.ManualReadOrder}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.UtcTimeStamp}, @{EventTable.Columns.Event}, @{EventTable.Columns.InsertAfter},@{EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces})
SET @{EventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.InsertedVersion, @event.InsertedVersion > @event.AggregateRootVersion ? @event.InsertedVersion : @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType, IdMapper.GetId(@event.GetType())));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, @event.EventId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.UtcTimeStamp, @event.UtcTimeStamp));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.ManualReadOrder, refactoringEvent.ManualReadOrder));

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, _eventSerializer.Serialize(@event)));

                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.ManualVersion, @event.ManualVersion)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertAfter, @event.InsertAfter)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertBefore, @event.InsertBefore)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.Replaces, @event.Replaces)));

                        var identityParameter = new SqlParameter(EventTable.Columns.InsertionOrder, SqlDbType.BigInt);
                        identityParameter.Direction = ParameterDirection.Output;

                        command.Parameters.Add(identityParameter);

                        command.ExecuteNonQuery();

                        @event.InsertionOrder = (long)identityParameter.Value;
                    }
                }
            }
        }

        public void InsertRefactoringEvents(IEnumerable<AggregateRootEvent> events)
        {
            var relatedEvents = LoadRelatedEventRefactoringInformation(events);


            var replacementGroup = events.Where(@event => @event.Replaces.HasValue).GroupBy(@event => @event.Replaces.Value).SingleOrDefault();
            var insertBeforeGroup = events.Where(@event => @event.InsertBefore.HasValue).GroupBy(@event => @event.InsertBefore.Value).SingleOrDefault();
            var insertAfterGroup = events.Where(@event => @event.InsertAfter.HasValue).GroupBy(@event => @event.InsertAfter.Value).SingleOrDefault();

            Contract.Assert(Seq.Create(replacementGroup, insertBeforeGroup, insertAfterGroup).Where(@this => @this != null).Count() == 1);

            if (replacementGroup != null)
            {
                var eventToReplace = relatedEvents.Single(@event => @event.InsertionOrder == replacementGroup.Key);

                SaveEventsWithinReadOrderRange(newEvents: replacementGroup.ToArray(), rangeStart: eventToReplace.EffectiveReadOrder, rangeEnd: eventToReplace.NextReadOrder);
            }
            else if (insertBeforeGroup != null)
            {
                var eventToInsertBefore = relatedEvents.Single(@event => @event.InsertionOrder == insertBeforeGroup.Key);

                SaveEventsWithinReadOrderRange(newEvents: insertBeforeGroup.ToArray(), rangeStart: eventToInsertBefore.PreviousReadOrder, rangeEnd: eventToInsertBefore.EffectiveReadOrder);
            }
            else if(insertAfterGroup != null)
            {
                var eventToInsertAfter = relatedEvents.Single(@event => @event.InsertionOrder == insertAfterGroup.Key);

                SaveEventsWithinReadOrderRange(newEvents: insertAfterGroup.ToArray(), rangeStart: eventToInsertAfter.EffectiveReadOrder, rangeEnd: eventToInsertAfter.NextReadOrder);
            }
        }


        private void SaveEventsWithinReadOrderRange(AggregateRootEvent[] newEvents, SqlDecimal rangeStart, SqlDecimal rangeEnd)
        {
            var increment = (rangeEnd - rangeStart)/(newEvents.Length + 1);

            IReadOnlyList<EventWithManualReadorder> eventsToPersist = newEvents.Select(
                (@event, index) => new EventWithManualReadorder
                                   {
                                       Event = @event,
                                       ManualReadOrder = rangeStart + (index + 1)*increment
                                   })
                                                                       .ToList();

            SaveEventsInternal(eventsToPersist);
        }

        internal void FixManualVersions(Guid aggregateId)
        {
            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = 
$@"
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
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, aggregateId));
                    command.ExecuteNonQuery();
                });    
        }

        private class EventWithManualReadorder
        {
            public SqlDecimal ManualReadOrder { get; set; }
            public AggregateRootEvent Event { get; set; }
        }

        private static SqlDecimal ToCorrectPrecisionAndScale(SqlDecimal value) { return SqlDecimal.ConvertToPrecScale(value, 38, 19); }

        private class EventOrderNeighbourhood
        {
            public long InsertionOrder { get; }
            public SqlDecimal EffectiveReadOrder { get; }
            public SqlDecimal PreviousReadOrder { get; }
            public SqlDecimal NextReadOrder { get; }            

            public EventOrderNeighbourhood(long insertionOrder, SqlDecimal effectiveReadOrder, SqlDecimal previousReadOrder, SqlDecimal nextReadOrder)
            {
                InsertionOrder = insertionOrder;
                EffectiveReadOrder = effectiveReadOrder;
                NextReadOrder = UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(nextReadOrder);
                PreviousReadOrder = UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(previousReadOrder);
            }

            private SqlDecimal UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(SqlDecimal previousReadOrder)
            {
                return previousReadOrder > 0 ? previousReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(0));
            }

            private SqlDecimal UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(SqlDecimal nextReadOrder)
            {
                return !nextReadOrder.IsNull ? nextReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(InsertionOrder + 1));
            }
        }

        private IReadOnlyList<EventOrderNeighbourhood> LoadRelatedEventRefactoringInformation(IEnumerable<AggregateRootEvent> events)
        {
            var insertBefore = events.Select(@this => @this.InsertBefore).Where(@this => @this != null).ToSet();
            var insertAfter = events.Select(@this => @this.InsertAfter).Where(@this => @this != null).ToSet();
            var replaces = events.Select(@this => @this.Replaces).Where(@this => @this != null).ToSet();

            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdatelockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement= $@"
SELECT  {EventTable.Columns.InsertionOrder},
        {EventTable.Columns.EffectiveReadOrder},        
        (select top 1 EffectiveReadorder from Event e1 where e1.EffectiveReadOrder < Event.EffectiveReadOrder order by EffectiveReadOrder desc) PreviousReadOrder,
        (select top 1 EffectiveReadorder from Event e1 where e1.EffectiveReadOrder > Event.EffectiveReadOrder order by EffectiveReadOrder) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdatelockOnInitialRead} ";



            var originalsStatement =
                $@"{selectStatement} where {EventTable.Columns.InsertionOrder} in ( {replaces.Concat(insertBefore).Concat(insertAfter)
                                                                                              .Select(@this => @this.ToString())
                                                                                              .Join(", ")} )";

            var relatedEvents = new List<EventOrderNeighbourhood>();

            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = originalsStatement;

                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            relatedEvents.Add(
                                new EventOrderNeighbourhood(
                                    insertionOrder: reader.GetInt64(0),
                                    effectiveReadOrder: reader.GetSqlDecimal(1),
                                    previousReadOrder: reader.GetSqlDecimal(2),
                                    nextReadOrder: reader.GetSqlDecimal(3)                                    
                                    ));
                        }
                    }
                });

            return relatedEvents;
        }

        private static SqlParameter Nullable(SqlParameter @this)
        {
            @this.IsNullable = true;
            @this.Direction = ParameterDirection.Input;
            if (@this.Value == null)
            {
                @this.Value = DBNull.Value;
            }
            return @this;
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText +=
                        $"DELETE {EventTable.Name} With(ROWLOCK) WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, aggregateId));
                    command.ExecuteNonQuery();
                });
        }
    }
}