using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.EventStore;
using Composable.System.Linq;

namespace Composable.Persistence.SqlServer.EventStore
{
    class SqlServerEventStoreEventWriter : IEventStoreEventWriter
    {
        const int PrimaryKeyViolationSqlErrorNumber = 2627;

        readonly SqlServerEventStoreConnectionManager _connectionManager;

        public SqlServerEventStoreEventWriter
            (SqlServerEventStoreConnectionManager connectionManager) => _connectionManager = connectionManager;

        public void Insert(IReadOnlyList<EventWriteDataRow> events)
        {
            using var connection = _connectionManager.OpenConnection();
            foreach(var data in events)
            {
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;

                command.CommandText +=
                    $@"
INSERT {SqlServerEventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {SqlServerEventTable.Columns.AggregateId},  {SqlServerEventTable.Columns.InsertedVersion},  {SqlServerEventTable.Columns.ManualVersion}, {SqlServerEventTable.Columns.EventType},  {SqlServerEventTable.Columns.EventId},  {SqlServerEventTable.Columns.UtcTimeStamp},  {SqlServerEventTable.Columns.Event}) 
VALUES(@{SqlServerEventTable.Columns.AggregateId}, @{SqlServerEventTable.Columns.InsertedVersion}, @{SqlServerEventTable.Columns.ManualVersion}, @{SqlServerEventTable.Columns.EventType}, @{SqlServerEventTable.Columns.EventId}, @{SqlServerEventTable.Columns.UtcTimeStamp}, @{SqlServerEventTable.Columns.Event})";

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier){Value = data.AggregateId });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.InsertedVersion, SqlDbType.Int) { Value = data.InsertedVersion });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventType,SqlDbType.UniqueIdentifier){Value = data.EventType.GuidValue });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventId, SqlDbType.UniqueIdentifier) {Value = data.EventId});
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.UtcTimeStamp, SqlDbType.DateTime2) {Value = data.UtcTimeStamp});

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.Event, SqlDbType.NVarChar, -1) {Value = data.EventJson});

                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.ManualVersion, SqlDbType.Int) {Value = data.ManualVersion}));

                try
                {
                    command.ExecuteNonQuery();
                }
                catch(SqlException e) when(e.Number == PrimaryKeyViolationSqlErrorNumber)
                {
                    throw new SqlServerEventStoreOptimisticConcurrencyException(e);
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

                SaveRefactoringEventsWithinReadOrderRange(
                    newEvents: replacementGroup.ToArray(),
                    rangeStart: eventToReplace.EffectiveReadOrder,
                    rangeEnd: eventToReplace.NextReadOrder);
            }
            else if (insertBeforeGroup != null)
            {
                Contract.Assert.That(insertBeforeGroup.All(@this => @this.InsertBefore.HasValue && @this.InsertBefore.Value > 0),
                                     "insertBeforeGroup.All(@this => @this.InsertBefore.HasValue && @this.InsertBefore.Value > 0)");
                var eventToInsertBefore = LoadEventOrderNeighborhood(insertBeforeGroup.Key);

                SaveRefactoringEventsWithinReadOrderRange(
                    newEvents: insertBeforeGroup.ToArray(),
                    rangeStart: eventToInsertBefore.PreviousReadOrder,
                    rangeEnd: eventToInsertBefore.EffectiveReadOrder);
            }
            else if (insertAfterGroup != null)
            {
                Contract.Assert.That(insertAfterGroup.All(@this => @this.InsertAfter.HasValue && @this.InsertAfter.Value > 0),
                                     "insertAfterGroup.All(@this => @this.InsertAfter.HasValue && @this.InsertAfter.Value > 0)");
                var eventToInsertAfter = LoadEventOrderNeighborhood(insertAfterGroup.Key);

                SaveRefactoringEventsWithinReadOrderRange(
                    newEvents: insertAfterGroup.ToArray(),
                    rangeStart: eventToInsertAfter.EffectiveReadOrder,
                    rangeEnd: eventToInsertAfter.NextReadOrder);
            }

            FixManualVersions(events.First().AggregateId);
        }

        void SaveRefactoringEventsWithinReadOrderRange(EventWriteDataRow[] newEvents, SqlDecimal rangeStart, SqlDecimal rangeEnd)
        {
            var increment = (rangeEnd - rangeStart) / (newEvents.Length + 1);

            for(int index = 0; index < newEvents.Length; ++index)
            {
                newEvents[index].ManualReadOrder = rangeStart + (index + 1) * increment;
            }

            using var connection = _connectionManager.OpenConnection();
            foreach(var data in newEvents)
            {
                using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;

                command.CommandText +=
                    $@"
INSERT {SqlServerEventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {SqlServerEventTable.Columns.AggregateId},  {SqlServerEventTable.Columns.InsertedVersion},  {SqlServerEventTable.Columns.ManualVersion},  {SqlServerEventTable.Columns.ManualReadOrder},  {SqlServerEventTable.Columns.EventType},  {SqlServerEventTable.Columns.EventId},  {SqlServerEventTable.Columns.UtcTimeStamp},  {SqlServerEventTable.Columns.Event},  {SqlServerEventTable.Columns.InsertAfter}, {SqlServerEventTable.Columns.InsertBefore},  {SqlServerEventTable.Columns.Replaces}) 
VALUES(@{SqlServerEventTable.Columns.AggregateId}, @{SqlServerEventTable.Columns.InsertedVersion}, @{SqlServerEventTable.Columns.ManualVersion}, @{SqlServerEventTable.Columns.ManualReadOrder}, @{SqlServerEventTable.Columns.EventType}, @{SqlServerEventTable.Columns.EventId}, @{SqlServerEventTable.Columns.UtcTimeStamp}, @{SqlServerEventTable.Columns.Event}, @{SqlServerEventTable.Columns.InsertAfter},@{SqlServerEventTable.Columns.InsertBefore}, @{SqlServerEventTable.Columns.Replaces})
SET @{SqlServerEventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier){Value = data.AggregateId });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.InsertedVersion, SqlDbType.Int) { Value = data.InsertedVersion });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventType,SqlDbType.UniqueIdentifier){Value = data.EventType.GuidValue });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventId, SqlDbType.UniqueIdentifier) {Value = data.EventId});
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.UtcTimeStamp, SqlDbType.DateTime2) {Value = data.UtcTimeStamp});
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.ManualReadOrder, SqlDbType.Decimal) {Value = data.ManualReadOrder});

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.Event, SqlDbType.NVarChar, -1) {Value = data.EventJson});

                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.ManualVersion, SqlDbType.Int) {Value = data.ManualVersion}));
                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.InsertAfter, SqlDbType.BigInt) {Value = data.InsertAfter}));
                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.InsertBefore, SqlDbType.BigInt) {Value = data.InsertBefore}));
                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.Replaces, SqlDbType.BigInt) {Value = data.Replaces}));

                var identityParameter = new SqlParameter(SqlServerEventTable.Columns.InsertionOrder, SqlDbType.BigInt)
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
                    throw new SqlServerEventStoreOptimisticConcurrencyException(e);
                }

                data.InsertionOrder = (long)identityParameter.Value;
            }
        }

        void FixManualVersions(Guid aggregateId)
        {
            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = SqlServerEventStore.SqlStatements.FixManualVersionsForAggregate;
                    command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
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
SELECT  {SqlServerEventTable.Columns.InsertionOrder},
        {SqlServerEventTable.Columns.EffectiveReadOrder},        
        (select top 1 {SqlServerEventTable.Columns.EffectiveReadOrder} from {SqlServerEventTable.Name} e1 where e1.{SqlServerEventTable.Columns.EffectiveReadOrder} < {SqlServerEventTable.Name}.{SqlServerEventTable.Columns.EffectiveReadOrder} order by {SqlServerEventTable.Columns.EffectiveReadOrder} desc) PreviousReadOrder,
        (select top 1 {SqlServerEventTable.Columns.EffectiveReadOrder} from {SqlServerEventTable.Name} e1 where e1.{SqlServerEventTable.Columns.EffectiveReadOrder} > {SqlServerEventTable.Name}.{SqlServerEventTable.Columns.EffectiveReadOrder} order by {SqlServerEventTable.Columns.EffectiveReadOrder}) NextReadOrder
FROM    {SqlServerEventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {SqlServerEventTable.Columns.InsertionOrder} = @{SqlServerEventTable.Columns.InsertionOrder}";




            EventOrderNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = selectStatement;
                    command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.InsertionOrder, SqlDbType.BigInt) {Value = insertionOrder});
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
                        $"DELETE {SqlServerEventTable.Name} With(ROWLOCK) WHERE {SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}";
                    command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }
    }
}
