using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Contracts;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreEventWriter : IEventStoreEventWriter
    {
        const int PrimaryKeyViolationSqlErrorNumber = 2627;

        readonly SqlServerEventStoreConnectionManager _connectionManager;
        IEventTypeToIdMapper IdMapper => _schemaManager.IdMapper;
        readonly IEventStoreSchemaManager _schemaManager;

        public SqlServerEventStoreEventWriter
            (SqlServerEventStoreConnectionManager connectionManager,
             IEventStoreSchemaManager schemaManager)
        {
            _connectionManager = connectionManager;
            _schemaManager = schemaManager;
        }

        public void Insert(IReadOnlyList<EventWriteDataRow> events)
        {
            //Pull types out here so that we do not open nested connections by calling into the IdMapper below. This will force DTC escalation of the transaction...
            var eventTypeToId = events.Select(@this => @this.Event.GetType())
                  .Distinct()
                  .ToDictionary(keySelector: @this => @this, elementSelector: @this => IdMapper.GetId(@this));

            using var connection = _connectionManager.OpenConnection();
            foreach(var data in events)
            {
                var @event = data.Event;
                @event.InsertedVersion = @event.InsertedVersion > @event.AggregateVersion ? @event.InsertedVersion : @event.AggregateVersion;

                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;

                command.CommandText +=
                    $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.InsertedVersion},  {EventTable.Columns.ManualVersion}, {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.UtcTimeStamp},  {EventTable.Columns.Event}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.InsertedVersion}, @{EventTable.Columns.ManualVersion}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.UtcTimeStamp}, @{EventTable.Columns.Event})";

                command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier){Value = @event.AggregateId });
                command.Parameters.Add(new SqlParameter(EventTable.Columns.InsertedVersion, SqlDbType.Int) { Value = @event.InsertedVersion });
                command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType,SqlDbType.Int){Value = eventTypeToId[@event.GetType()] });
                command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, SqlDbType.UniqueIdentifier) {Value = @event.EventId});
                command.Parameters.Add(new SqlParameter(EventTable.Columns.UtcTimeStamp, SqlDbType.DateTime2) {Value = @event.UtcTimeStamp});

                command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, SqlDbType.NVarChar, -1) {Value = data.EventJson});

                command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.ManualVersion, SqlDbType.Int) {Value = @event.ManualVersion}));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch(SqlException e) when(e.Number == PrimaryKeyViolationSqlErrorNumber)
                {
                    throw new EventStoreOptimisticConcurrencyException(e);
                }
            }
        }

        public void InsertRefactoringEvents(IReadOnlyList<EventWriteDataRow> events)
        {
            // ReSharper disable PossibleInvalidOperationException
            var replacementGroup = events.Where(@event => @event.Replaces.HasValue)
                                         .GroupBy(@event => @event.Replaces!.Value)
                                         .SingleOrDefault();
            var insertBeforeGroup = events.Where(@event => @event.InsertBefore.HasValue)
                                          .GroupBy(@event => @event.InsertBefore!.Value)
                                          .SingleOrDefault();
            var insertAfterGroup = events.Where(@event => @event.InsertAfter.HasValue)
                                         .GroupBy(@event => @event.InsertAfter!.Value)
                                         .SingleOrDefault();
            // ReSharper restore PossibleInvalidOperationException

            Contract.Assert.That(Seq.Create(replacementGroup, insertBeforeGroup, insertAfterGroup).Where(@this => @this != null).Count() == 1,
                                 "Seq.Create(replacementGroup, insertBeforeGroup, insertAfterGroup).Where(@this => @this != null).Count() == 1");

            if (replacementGroup != null)
            {
                Contract.Assert.That(replacementGroup.All(@this => @this.Replaces.HasValue && @this.Replaces > 0),
                                     "replacementGroup.All(@this => @this.Replaces.HasValue && @this.Replaces > 0)");
                var eventToReplace = LoadEventOrderNeighborhood(replacementGroup.Key);

                SaveEventsWithinReadOrderRange(
                    newEvents: replacementGroup.ToArray(),
                    rangeStart: eventToReplace.EffectiveReadOrder,
                    rangeEnd: eventToReplace.NextReadOrder);
            }
            else if (insertBeforeGroup != null)
            {
                Contract.Assert.That(insertBeforeGroup.All(@this => @this.InsertBefore.HasValue && @this.InsertBefore.Value > 0),
                                     "insertBeforeGroup.All(@this => @this.InsertBefore.HasValue && @this.InsertBefore.Value > 0)");
                var eventToInsertBefore = LoadEventOrderNeighborhood(insertBeforeGroup.Key);

                SaveEventsWithinReadOrderRange(
                    newEvents: insertBeforeGroup.ToArray(),
                    rangeStart: eventToInsertBefore.PreviousReadOrder,
                    rangeEnd: eventToInsertBefore.EffectiveReadOrder);
            }
            else if (insertAfterGroup != null)
            {
                Contract.Assert.That(insertAfterGroup.All(@this => @this.InsertAfter.HasValue && @this.InsertAfter.Value > 0),
                                     "insertAfterGroup.All(@this => @this.InsertAfter.HasValue && @this.InsertAfter.Value > 0)");
                var eventToInsertAfter = LoadEventOrderNeighborhood(insertAfterGroup.Key);

                SaveEventsWithinReadOrderRange(
                    newEvents: insertAfterGroup.ToArray(),
                    rangeStart: eventToInsertAfter.EffectiveReadOrder,
                    rangeEnd: eventToInsertAfter.NextReadOrder);
            }

            FixManualVersions(events.First().AggregateId);
        }

        void SaveEventsWithinReadOrderRange(EventWriteDataRow[] newEvents, SqlDecimal rangeStart, SqlDecimal rangeEnd)
        {
            var increment = (rangeEnd - rangeStart) / (newEvents.Length + 1);

            IReadOnlyList<EventWriteDataRow> eventsToPersist = newEvents.Select(
                                                                            (eventDataRow, index) => new EventWriteDataRow(source: eventDataRow, manualReadOrder: rangeStart + (index + 1) * increment))
                                                                        .ToList();

            //Pull types out here so that we do not open nested connections by calling into the IdMapper below. This will force DTC escalation of the transaction...
            var eventTypeToId = eventsToPersist.Select(@this => @this.Event.GetType())
                  .Distinct()
                  .ToDictionary(keySelector: @this => @this, elementSelector: @this => IdMapper.GetId(@this));

            using var connection = _connectionManager.OpenConnection();
            foreach(var data in eventsToPersist)
            {
                var @event = data.Event;
                @event.InsertedVersion = data.InsertedVersion > data.AggregateVersion ? data.InsertedVersion : data.AggregateVersion;

                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;

                command.CommandText +=
                    $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.InsertedVersion},  {EventTable.Columns.ManualVersion},  {EventTable.Columns.ManualReadOrder},  {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.UtcTimeStamp},  {EventTable.Columns.Event},  {EventTable.Columns.InsertAfter}, {EventTable.Columns.InsertBefore},  {EventTable.Columns.Replaces}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.InsertedVersion}, @{EventTable.Columns.ManualVersion}, @{EventTable.Columns.ManualReadOrder}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.UtcTimeStamp}, @{EventTable.Columns.Event}, @{EventTable.Columns.InsertAfter},@{EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces})
SET @{EventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier){Value = data.AggregateId });
                command.Parameters.Add(new SqlParameter(EventTable.Columns.InsertedVersion, SqlDbType.Int) { Value = @event.InsertedVersion });
                command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType,SqlDbType.Int){Value = eventTypeToId[@event.GetType()] });
                command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, SqlDbType.UniqueIdentifier) {Value = data.EventId});
                command.Parameters.Add(new SqlParameter(EventTable.Columns.UtcTimeStamp, SqlDbType.DateTime2) {Value = data.UtcTimeStamp});
                command.Parameters.Add(new SqlParameter(EventTable.Columns.ManualReadOrder, SqlDbType.Decimal) {Value = data.ManualReadOrder});

                command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, SqlDbType.NVarChar, -1) {Value = data.EventJson});

                command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.ManualVersion, SqlDbType.Int) {Value = data.ManualVersion}));
                command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertAfter, SqlDbType.BigInt) {Value = data.InsertAfter}));
                command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertBefore, SqlDbType.BigInt) {Value = data.InsertBefore}));
                command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.Replaces, SqlDbType.BigInt) {Value = data.Replaces}));

                var identityParameter = new SqlParameter(EventTable.Columns.InsertionOrder, SqlDbType.BigInt)
                                        {
                                            Direction = ParameterDirection.Output
                                        };

                command.Parameters.Add(identityParameter);

                try
                {
                    command.ExecuteNonQuery();
                }
                catch(SqlException e) when(e.Number == PrimaryKeyViolationSqlErrorNumber)
                {
                    throw new EventStoreOptimisticConcurrencyException(e);
                }

                data.InsertionOrder = @event.InsertionOrder = (long)identityParameter.Value;
            }
        }

        void FixManualVersions(Guid aggregateId)
        {
            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = SqlServerEventStore.SqlStatements.FixManualVersionsForAggregate;
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }

        static SqlDecimal ToCorrectPrecisionAndScale(SqlDecimal value) => SqlDecimal.ConvertToPrecScale(value, 38, 19);

        class EventOrderNeighborhood
        {
            long InsertionOrder { get; }
            public SqlDecimal EffectiveReadOrder { get; }
            public SqlDecimal PreviousReadOrder { get; }
            public SqlDecimal NextReadOrder { get; }

            public EventOrderNeighborhood(long insertionOrder, SqlDecimal effectiveReadOrder, SqlDecimal previousReadOrder, SqlDecimal nextReadOrder)
            {
                InsertionOrder = insertionOrder;
                EffectiveReadOrder = effectiveReadOrder;
                NextReadOrder = UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(nextReadOrder);
                PreviousReadOrder = UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(previousReadOrder);
            }

            static SqlDecimal UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(SqlDecimal previousReadOrder) => previousReadOrder > 0 ? previousReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(0));

            SqlDecimal UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(SqlDecimal nextReadOrder) => !nextReadOrder.IsNull ? nextReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(InsertionOrder + 1));
        }

        EventOrderNeighborhood LoadEventOrderNeighborhood(long insertionOrder)
        {
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {EventTable.Columns.InsertionOrder},
        {EventTable.Columns.EffectiveReadOrder},        
        (select top 1 {EventTable.Columns.EffectiveReadOrder} from {EventTable.Name} e1 where e1.{EventTable.Columns.EffectiveReadOrder} < {EventTable.Name}.{EventTable.Columns.EffectiveReadOrder} order by {EventTable.Columns.EffectiveReadOrder} desc) PreviousReadOrder,
        (select top 1 {EventTable.Columns.EffectiveReadOrder} from {EventTable.Name} e1 where e1.{EventTable.Columns.EffectiveReadOrder} > {EventTable.Name}.{EventTable.Columns.EffectiveReadOrder} order by {EventTable.Columns.EffectiveReadOrder}) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {EventTable.Columns.InsertionOrder} = @{EventTable.Columns.InsertionOrder}";




            EventOrderNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = selectStatement;
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.InsertionOrder, SqlDbType.BigInt) {Value = insertionOrder});
                    using var reader = command.ExecuteReader();
                    reader.Read();

                    neighborhood = new EventOrderNeighborhood(
                        insertionOrder: reader.GetInt64(0),
                        effectiveReadOrder: reader.GetSqlDecimal(1),
                        previousReadOrder: reader.GetSqlDecimal(2),
                        nextReadOrder: reader.GetSqlDecimal(3));
                });

            return Assert.Result.NotNull(neighborhood);
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
            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText +=
                        $"DELETE {EventTable.Name} With(ROWLOCK) WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }
    }
}
