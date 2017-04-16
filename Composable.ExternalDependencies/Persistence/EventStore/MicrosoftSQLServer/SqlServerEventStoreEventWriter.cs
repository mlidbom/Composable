using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreEventWriter : IEventStoreEventWriter
    {
        readonly SqlServerEventStoreConnectionManager _connectionMananger;
        IEventTypeToIdMapper IdMapper => _schemaManager.IdMapper;
        readonly IEventStoreSchemaManager _schemaManager;

        public SqlServerEventStoreEventWriter
            (SqlServerEventStoreConnectionManager connectionMananger,
             IEventStoreSchemaManager schemaManager)
        {
            _connectionMananger = connectionMananger;
            _schemaManager = schemaManager;
        }

        public void Insert(IEnumerable<EventWriteDataRow> events)
        {
            using(var connection = _connectionMananger.OpenConnection())
            {
                foreach(var data in events)
                {
                    var @event = data.Event;
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;

                        command.CommandText +=
                            $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.InsertedVersion},  {EventTable.Columns.ManualVersion}, {EventTable.Columns.ManualReadOrder}, {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.UtcTimeStamp},  {EventTable.Columns.Event},  {EventTable.Columns.InsertAfter}, {EventTable.Columns.InsertBefore},  {EventTable.Columns.Replaces}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.InsertedVersion}, @{EventTable.Columns.ManualVersion}, @{EventTable.Columns.ManualReadOrder}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.UtcTimeStamp}, @{EventTable.Columns.Event}, @{EventTable.Columns.InsertAfter},@{EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces})
SET @{EventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, data.AggregateRootId));
                        command.Parameters.Add(
                            new SqlParameter(
                                EventTable.Columns.InsertedVersion,
                                data.InsertedVersion > data.AggregateRootVersion ? data.InsertedVersion : data.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType, IdMapper.GetId(@event.GetType())));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, data.EventId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.UtcTimeStamp, data.UtcTimeStamp));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.ManualReadOrder, data.ManualReadOrder));

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, data.EventJson));

                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.ManualVersion, data.ManualVersion)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertAfter, data.InsertAfter)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertBefore, data.InsertBefore)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.Replaces, data.Replaces)));

                        var identityParameter = new SqlParameter(EventTable.Columns.InsertionOrder, SqlDbType.BigInt)
                                                {
                                                    Direction = ParameterDirection.Output
                                                };

                        command.Parameters.Add(identityParameter);

                        command.ExecuteNonQuery();

                        data.InsertionOrder = @event.InsertionOrder = (long)identityParameter.Value;
                    }
                }
            }
        }

        public void InsertRefactoringEvents(IReadOnlyList<EventWriteDataRow> events)
        {
            var relatedEvents = LoadRelatedEventRefactoringInformation(events);

            var replacementGroup = events.Where(@event => @event.Replaces.HasValue)
                                         .GroupBy(@event => @event.Replaces.Value)
                                         .SingleOrDefault();
            var insertBeforeGroup = events.Where(@event => @event.InsertBefore.HasValue)
                                          .GroupBy(@event => @event.InsertBefore.Value)
                                          .SingleOrDefault();
            var insertAfterGroup = events.Where(@event => @event.InsertAfter.HasValue)
                                         .GroupBy(@event => @event.InsertAfter.Value)
                                         .SingleOrDefault();

            Contract.Assert.That(Seq.Create(replacementGroup, insertBeforeGroup, insertAfterGroup).Where(@this => @this != null).Count() == 1,
                                 "Seq.Create(replacementGroup, insertBeforeGroup, insertAfterGroup).Where(@this => @this != null).Count() == 1");

            if (replacementGroup != null)
            {
                Contract.Assert.That(replacementGroup.All(@this => @this.Replaces.HasValue && @this.Replaces > 0),
                                     "replacementGroup.All(@this => @this.Replaces.HasValue && @this.Replaces > 0)");
                var eventToReplace = relatedEvents.Single(@event => @event.InsertionOrder == replacementGroup.Key);

                SaveEventsWithinReadOrderRange(
                    newEvents: replacementGroup.ToArray(),
                    rangeStart: eventToReplace.EffectiveReadOrder,
                    rangeEnd: eventToReplace.NextReadOrder);
            }
            else if (insertBeforeGroup != null)
            {
                Contract.Assert.That(insertBeforeGroup.All(@this => @this.InsertBefore.HasValue && @this.InsertBefore.Value > 0),
                                     "insertBeforeGroup.All(@this => @this.InsertBefore.HasValue && @this.InsertBefore.Value > 0)");
                var eventToInsertBefore = relatedEvents.Single(@event => @event.InsertionOrder == insertBeforeGroup.Key);

                SaveEventsWithinReadOrderRange(
                    newEvents: insertBeforeGroup.ToArray(),
                    rangeStart: eventToInsertBefore.PreviousReadOrder,
                    rangeEnd: eventToInsertBefore.EffectiveReadOrder);
            }
            else if (insertAfterGroup != null)
            {
                Contract.Assert.That(insertAfterGroup.All(@this => @this.InsertAfter.HasValue && @this.InsertAfter.Value > 0),
                                     "insertAfterGroup.All(@this => @this.InsertAfter.HasValue && @this.InsertAfter.Value > 0)");
                var eventToInsertAfter = relatedEvents.Single(@event => @event.InsertionOrder == insertAfterGroup.Key);

                SaveEventsWithinReadOrderRange(
                    newEvents: insertAfterGroup.ToArray(),
                    rangeStart: eventToInsertAfter.EffectiveReadOrder,
                    rangeEnd: eventToInsertAfter.NextReadOrder);
            }

            FixManualVersions(events.First().AggregateRootId);
        }

        void SaveEventsWithinReadOrderRange(EventWriteDataRow[] newEvents, SqlDecimal rangeStart, SqlDecimal rangeEnd)
        {
            var increment = (rangeEnd - rangeStart) / (newEvents.Length + 1);

            IReadOnlyList<EventWriteDataRow> eventsToPersist = newEvents.Select(
                                                                            (eventDataRow, index) => new EventWriteDataRow(source: eventDataRow, manualReadOrder: rangeStart + (index + 1) * increment))
                                                                        .ToList();

            Insert(eventsToPersist);
        }

        void FixManualVersions(Guid aggregateId)
        {
            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = SqlServerEventStore.SqlStatements.FixManualVersionsForAggregate(aggregateId);
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, aggregateId));
                    command.ExecuteNonQuery();
                });
        }

        static SqlDecimal ToCorrectPrecisionAndScale(SqlDecimal value) => SqlDecimal.ConvertToPrecScale(value, 38, 19);

        class EventOrderNeighbourhood
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

            static SqlDecimal UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(SqlDecimal previousReadOrder) => previousReadOrder > 0 ? previousReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(0));

            SqlDecimal UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(SqlDecimal nextReadOrder) => !nextReadOrder.IsNull ? nextReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(InsertionOrder + 1));
        }

        IReadOnlyList<EventOrderNeighbourhood> LoadRelatedEventRefactoringInformation(IReadOnlyList<EventWriteDataRow> events)
        {
            var relatedEventsInsertionOrderList = events.SelectMany(@this => Seq.Create(@this.InsertBefore, @this.InsertAfter, @this.Replaces))
                                                        .Where(@this => @this != null)
                                                        .Select(@this => @this.ToString())
                                                        .Distinct()
                                                        .Join(", ");

            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdatelockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {EventTable.Columns.InsertionOrder},
        {EventTable.Columns.EffectiveReadOrder},        
        (select top 1 EffectiveReadorder from Event e1 where e1.EffectiveReadOrder < Event.EffectiveReadOrder order by EffectiveReadOrder desc) PreviousReadOrder,
        (select top 1 EffectiveReadorder from Event e1 where e1.EffectiveReadOrder > Event.EffectiveReadOrder order by EffectiveReadOrder) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdatelockOnInitialRead} ";



            var originalsStatement =
                $@"{selectStatement} where {EventTable.Columns.InsertionOrder} in ( {relatedEventsInsertionOrderList} )";

            var relatedEvents = new List<EventOrderNeighbourhood>();

            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = originalsStatement;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
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

        static SqlParameter Nullable(SqlParameter @this)
        {
            @this.IsNullable = true;
            @this.Direction = ParameterDirection.Input;
            if(@this.Value == null)
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
