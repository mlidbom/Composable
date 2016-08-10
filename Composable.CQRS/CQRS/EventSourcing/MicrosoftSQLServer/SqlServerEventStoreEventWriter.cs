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
            using(var connection = _connectionMananger.OpenConnection())
            {
                foreach(var @event in events)
                {
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;

                        command.CommandText +=
                            $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.InsertedVersion}, {EventTable.Columns.ManualVersion},  {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.UtcTimeStamp},  {EventTable.Columns.Event},  {EventTable.Columns.InsertAfter}, {EventTable.Columns.InsertBefore},  {EventTable.Columns.Replaces}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.InsertedVersion}, @{EventTable.Columns.ManualVersion}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.UtcTimeStamp}, @{EventTable.Columns.Event}, @{EventTable.Columns.InsertAfter},@{EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces})
SET @{EventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.InsertedVersion, @event.InsertedVersion >  @event.AggregateRootVersion ? @event.InsertedVersion : @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType, IdMapper.GetId(@event.GetType())));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, @event.EventId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.UtcTimeStamp, @event.UtcTimeStamp));

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

        private void SaveRefactoringEvents(IEnumerable<RefactoringEvent> events)
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
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.InsertedVersion},  {EventTable.Columns.ManualReadOrder}, {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.UtcTimeStamp},  {EventTable.Columns.Event},  {EventTable.Columns.InsertAfter}, {EventTable.Columns.InsertBefore},  {EventTable.Columns.Replaces}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.InsertedVersion}, @{EventTable.Columns.ManualReadOrder}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.UtcTimeStamp}, @{EventTable.Columns.Event}, @{EventTable.Columns.InsertAfter},@{EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces})
SET @{EventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.InsertedVersion, @event.InsertedVersion > @event.AggregateRootVersion ? @event.InsertedVersion : @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType, IdMapper.GetId(@event.GetType())));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, @event.EventId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.UtcTimeStamp, @event.UtcTimeStamp));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.ManualReadOrder, refactoringEvent.ManualReadOrder));

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, _eventSerializer.Serialize(@event)));

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

            IReadOnlyList<RefactoringEvent> eventsToPersist = null;
            SqlDecimal startRange;
            SqlDecimal endRange;

            AggregateRootEvent[] newEvents;
            if (replacementGroup != null)
            {
                Debug.WriteLine("#####Replace");
                var eventToReplace = relatedEvents.Single(@event => @event.InsertionOrder == replacementGroup.Key);
                Contract.Assert(relatedEvents.None(@event => @event.Replaces.HasValue && @event.Replaces == eventToReplace.InsertionOrder));

                startRange = eventToReplace.EffectiveReadOrder;
                endRange = eventToReplace.NextReadOrder;
                newEvents = replacementGroup.ToArray();
            }else if (insertBeforeGroup != null)
            {
                Debug.WriteLine("#####Insert Before");
                var eventToInsertBefore = relatedEvents.Single(@event => @event.InsertionOrder == insertBeforeGroup.Key);

                endRange = eventToInsertBefore.EffectiveReadOrder;
                startRange = eventToInsertBefore.PreviousReadOrder;
                newEvents = insertBeforeGroup.ToArray();
            }else if(insertAfterGroup != null)
            {
                Debug.WriteLine("#####Insert After");
                var eventToInsertAfter = relatedEvents.Single(@event => @event.InsertionOrder == insertAfterGroup.Key);

                startRange = eventToInsertAfter.EffectiveReadOrder;
                endRange = eventToInsertAfter.NextReadOrder;
                newEvents = insertAfterGroup.ToArray();
            }
            else
            {
                throw new Exception("WTF?");
            }

            var increment = (endRange - startRange) / (newEvents.Length + 1);

            Debug.WriteLine($"{nameof(startRange)}: {startRange}, {nameof(endRange)}: {endRange}, {nameof(increment)}: {increment}");
            eventsToPersist =
                newEvents.Select(
                    (@event, index) => new RefactoringEvent
                    {
                        Event = @event,
                        ManualReadOrder = startRange + (index + 1) * increment
                    })
                         .ToList();

            eventsToPersist.ForEach(@this => Debug.WriteLine(@this.ToNewtonSoftDebugString(Formatting.None)));

            SaveRefactoringEvents(eventsToPersist);
        }

        internal void FixManualVersions(Guid aggregateId)
        {
            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = 
$@"

--update Event set ManualVersion = null

update replaced
set replaced.ManualReadOrder = -abs(replaced.EffectiveReadOrder)
from Event replaced
inner join Event replaces
	on replaces.Replaces = replaced.InsertionOrder
where replaces.Replaces is not null
and (replaced.ManualReadOrder > 0 or replaced.ManualReadOrder is null)

update {EventTable.Name} 
set {EventTable.Columns.ManualVersion} = ChangedReadOrders.NewVersion
from {EventTable.Name} 
	inner join 
(
	select * from
	(select e.{EventTable.Columns.AggregateId}, {EventTable.Columns.InsertedVersion}, row_number() over (partition by e.{EventTable.Columns.AggregateId} order by e.{EventTable.Columns.EffectiveReadOrder}) NewVersion, {EventTable.Columns.EffectiveVersion}
	    from {EventTable.Name} e
	    where e.{EventTable.Columns.EffectiveReadOrder} > 0) NewReadOrders
	where NewReadOrders.{EventTable.Columns.EffectiveVersion} is null or ( NewReadOrders.NewVersion != NewReadOrders.{EventTable.Columns.EffectiveVersion})
) ChangedReadOrders

on {EventTable.Name}.{EventTable.Columns.AggregateId} = ChangedReadOrders.{EventTable.Columns.AggregateId} and {EventTable.Name}.{EventTable.Columns.InsertedVersion} = ChangedReadOrders.{EventTable.Columns.InsertedVersion}


update {EventTable.Name}
set {EventTable.Columns.ManualVersion} = -{EventTable.Columns.InsertedVersion}
where ({EventTable.Columns.EffectiveVersion} > 0 or {EventTable.Columns.EffectiveVersion} is null) and {EventTable.Columns.EffectiveReadOrder} < 0
";
                    command.ExecuteNonQuery();
                });    
        }

        private class RefactoringEvent
        {
            public SqlDecimal ManualReadOrder { get; set; }
            public AggregateRootEvent Event { get; set; }
        }

        private static SqlDecimal ToCorrectPrecisionAndScale(SqlDecimal value) { return SqlDecimal.ConvertToPrecScale(value, 38, 19); }

        private class EventRefactoringInformation
        {
            public long InsertionOrder { get; }
            public SqlDecimal EffectiveReadOrder { get; }
            public SqlDecimal ManualReadOrder { get; }
            public int? EffectiveVersion { get; }
            public int InsertedVersion { get; }
            public int? ManualVersion { get; }            
            public long? Replaces { get; }
            public long? InsertAfter { get; }
            public long? InsertBefore { get; }
            public SqlDecimal PreviousReadOrder { get; }
            public SqlDecimal NextReadOrder { get; }            

            public EventRefactoringInformation(long insertionOrder, SqlDecimal effectiveReadOrder, SqlDecimal manualReadOrder, int? effectiveVersion, int insertedVersion, int? manualVersion, long? replaces, long? insertAfter, long? insertBefore, SqlDecimal previousReadOrder, SqlDecimal nextReadOrder)
            {
                InsertionOrder = insertionOrder;
                ManualReadOrder = manualReadOrder;
                EffectiveReadOrder = effectiveReadOrder;
                EffectiveVersion = effectiveVersion;
                InsertedVersion = insertedVersion;
                ManualVersion = manualVersion;
                Replaces = replaces;
                InsertAfter = insertAfter;
                InsertBefore = insertBefore;
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

        private IReadOnlyList<EventRefactoringInformation> LoadRelatedEventRefactoringInformation(IEnumerable<AggregateRootEvent> events)
        {
            var insertBefore = events.Select(@this => @this.InsertBefore).Where(@this => @this != null).ToSet();
            var insertAfter = events.Select(@this => @this.InsertAfter).Where(@this => @this != null).ToSet();
            var replaces = events.Select(@this => @this.Replaces).Where(@this => @this != null).ToSet();

            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdatelockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement= $@"
SELECT  {EventTable.Columns.InsertionOrder},
        {EventTable.Columns.EffectiveReadOrder},        
        {EventTable.Columns.ManualReadOrder}, 
        {EventTable.Columns.EffectiveVersion}, 
        {EventTable.Columns.InsertedVersion}, 
        {EventTable.Columns.ManualVersion}, 
        {EventTable.Columns.Replaces}, 
        {EventTable.Columns.InsertAfter}, 
        {EventTable.Columns.InsertBefore},
        (select top 1 EffectiveReadorder from Event e1 where e1.EffectiveReadOrder < Event.EffectiveReadOrder order by EffectiveReadOrder desc) PreviousReadOrder,
        (select top 1 EffectiveReadorder from Event e1 where e1.EffectiveReadOrder > Event.EffectiveReadOrder order by EffectiveReadOrder) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdatelockOnInitialRead} ";


            var insertBeforeFetchStatement = insertBefore.Any() 
                ? $@"{selectStatement} where {EventTable.Columns.InsertBefore} in ( {insertBefore.Select(@this => @this.ToString()).Join(", ")} )"
                : null ;

            var insertAfterStatement = insertAfter.Any()
                ? $@"{selectStatement} where {EventTable.Columns.InsertAfter} in ( {insertAfter.Select(@this => @this.ToString()).Join(", ")} )"
                : null;

            var replacesStatement = replaces.Any()
                ? $@"{selectStatement} where {EventTable.Columns.Replaces} in ( {replaces.Select(@this => @this.ToString()).Join(", ")} )"
                : null;

            var originalsStatement =
                $@"{selectStatement} where {EventTable.Columns.InsertedVersion} in ( {replaces.Concat(insertBefore).Concat(insertAfter)
                                                                                              .Select(@this => @this.ToString())
                                                                                              .Join(", ")} )";

            var unionStatement = Seq.Create(insertBeforeFetchStatement, insertAfterStatement, replacesStatement, originalsStatement)
                                    .Where(statement => statement != null)
                                    .Join($"{Environment.NewLine}union{Environment.NewLine}");

            var relatedEvents = new List<EventRefactoringInformation>();

            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = unionStatement;

                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            relatedEvents.Add(
                                new EventRefactoringInformation(
                                    insertionOrder: reader.GetInt64(0),
                                    effectiveReadOrder: reader.GetSqlDecimal(1),
                                    manualReadOrder: reader.GetSqlDecimal(2),
                                    effectiveVersion: reader[3] as int?,
                                    insertedVersion: reader.GetInt32(4),
                                    manualVersion: reader[5] as int?,
                                    replaces: reader[6] as long?,
                                    insertAfter: reader[7] as long?,
                                    insertBefore: reader[8] as long?,
                                    previousReadOrder: reader.GetSqlDecimal(9),
                                    nextReadOrder: reader.GetSqlDecimal(10)                                    
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