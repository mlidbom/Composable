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
    partial class SqlServerEventStorePersistenceLayer
    {
        const int PrimaryKeyViolationSqlErrorNumber = 2627;

        public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events)
        {
            using var connection = _connectionManager.OpenConnection();
            foreach(var data in events)
            {
                using var command = connection.CreateCommand();

                command.CommandText +=
                    $@"
INSERT {SqlServerEventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {SqlServerEventTable.Columns.AggregateId},  {SqlServerEventTable.Columns.InsertedVersion},  {SqlServerEventTable.Columns.ManualVersion}, {SqlServerEventTable.Columns.EventType},  {SqlServerEventTable.Columns.EventId},  {SqlServerEventTable.Columns.UtcTimeStamp},  {SqlServerEventTable.Columns.Event}) 
VALUES(@{SqlServerEventTable.Columns.AggregateId}, @{SqlServerEventTable.Columns.InsertedVersion}, @{SqlServerEventTable.Columns.ManualVersion}, @{SqlServerEventTable.Columns.EventType}, @{SqlServerEventTable.Columns.EventId}, @{SqlServerEventTable.Columns.UtcTimeStamp}, @{SqlServerEventTable.Columns.Event})";

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier){Value = data.AggregateId });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.InsertedVersion, SqlDbType.Int) { Value = data.RefactoringInformation.InsertedVersion });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventType,SqlDbType.UniqueIdentifier){Value = data.EventType });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventId, SqlDbType.UniqueIdentifier) {Value = data.EventId});
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.UtcTimeStamp, SqlDbType.DateTime2) {Value = data.UtcTimeStamp});

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.Event, SqlDbType.NVarChar, -1) {Value = data.EventJson});

                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.ManualVersion, SqlDbType.Int) {Value = data.RefactoringInformation.ManualVersion}));

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

        public void InsertAfterEvent(Guid eventId, EventDataRow[] insertAfterGroup)
        {
            var eventToInsertAfter = LoadEventInsertedBeforeAndAfter(eventId);

            SaveRefactoringEventsWithinReadOrderRange(
                newEvents: insertAfterGroup,
                rangeStart: eventToInsertAfter.EffectiveReadOrder,
                rangeEnd: eventToInsertAfter.NextEventReadOrder);
        }

        public void InsertBeforeEvent(Guid eventId, EventDataRow[] insertBeforeGroup)
        {
            var eventToInsertBefore = LoadEventInsertedBeforeAndAfter(eventId);

            SaveRefactoringEventsWithinReadOrderRange(
                newEvents: insertBeforeGroup,
                rangeStart: eventToInsertBefore.PreviousEventReadOrder,
                rangeEnd: eventToInsertBefore.EffectiveReadOrder);
        }

        public void ReplaceEvent(Guid eventId, EventDataRow[] replacementGroup)
        {
            var eventToReplace = LoadEventInsertedBeforeAndAfter(eventId);

            SaveRefactoringEventsWithinReadOrderRange(
                newEvents: replacementGroup,
                rangeStart: eventToReplace.EffectiveReadOrder,
                rangeEnd: eventToReplace.NextEventReadOrder);
        }

        void SaveRefactoringEventsWithinReadOrderRange(EventDataRow[] newEvents, SqlDecimal rangeStart, SqlDecimal rangeEnd)
        {
            var readOrderIncrement = (rangeEnd - rangeStart) / (newEvents.Length + 1);

            using var connection = _connectionManager.OpenConnection();
            for(int index = 0; index < newEvents.Length; ++index)
            {
                var data = newEvents[index];
                using var command = connection.CreateCommand();

                command.CommandText +=
                    $@"
INSERT {SqlServerEventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {SqlServerEventTable.Columns.AggregateId},  {SqlServerEventTable.Columns.InsertedVersion},  {SqlServerEventTable.Columns.ManualVersion},  {SqlServerEventTable.Columns.ManualReadOrder},  {SqlServerEventTable.Columns.EventType},  {SqlServerEventTable.Columns.EventId},  {SqlServerEventTable.Columns.UtcTimeStamp},  {SqlServerEventTable.Columns.Event},  {SqlServerEventTable.Columns.InsertAfter}, {SqlServerEventTable.Columns.InsertBefore},  {SqlServerEventTable.Columns.Replaces}) 
VALUES(@{SqlServerEventTable.Columns.AggregateId}, @{SqlServerEventTable.Columns.InsertedVersion}, @{SqlServerEventTable.Columns.ManualVersion}, @{SqlServerEventTable.Columns.ManualReadOrder}, @{SqlServerEventTable.Columns.EventType}, @{SqlServerEventTable.Columns.EventId}, @{SqlServerEventTable.Columns.UtcTimeStamp}, @{SqlServerEventTable.Columns.Event}, @{SqlServerEventTable.Columns.InsertAfter},@{SqlServerEventTable.Columns.InsertBefore}, @{SqlServerEventTable.Columns.Replaces})
SET @{SqlServerEventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier){Value = data.AggregateId });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.InsertedVersion, SqlDbType.Int) { Value = data.RefactoringInformation.InsertedVersion });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventType,SqlDbType.UniqueIdentifier){Value = data.EventType });
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventId, SqlDbType.UniqueIdentifier) {Value = data.EventId});
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.UtcTimeStamp, SqlDbType.DateTime2) {Value = data.UtcTimeStamp});

                //Urgent: Change this to another data type. https://github.com/mlidbom/Composable/issues/46
                var manualReadOrder = rangeStart + (index + 1) * readOrderIncrement;
                if(!(manualReadOrder.IsNull || (manualReadOrder.Precision == 38 && manualReadOrder.Scale == 17)))
                {
                    throw new ArgumentException($"$$$$$$$$$$$$$$$$$$$$$$$$$ Found decimal with precision: {manualReadOrder.Precision} and scale: {manualReadOrder.Scale}", nameof(manualReadOrder));
                }
                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.ManualReadOrder, SqlDbType.Decimal) {Value = manualReadOrder});

                command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.Event, SqlDbType.NVarChar, -1) {Value = data.EventJson});

                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.ManualVersion, SqlDbType.Int) {Value = data.RefactoringInformation.ManualVersion}));
                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.InsertAfter, SqlDbType.UniqueIdentifier) {Value = data.RefactoringInformation.InsertAfter}));
                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.InsertBefore, SqlDbType.UniqueIdentifier) {Value = data.RefactoringInformation.InsertBefore}));
                command.Parameters.Add(Nullable(new SqlParameter(SqlServerEventTable.Columns.Replaces, SqlDbType.UniqueIdentifier) {Value = data.RefactoringInformation.Replaces}));

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

        //Urgent: Do this logic in C# in the EventStore class. Persistence layer should only save the data, not implement logic that can be common for all persistence layers.
        public void FixManualVersions(Guid aggregateId)
        {
            _connectionManager.UseCommand(
                command =>
                {
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
            public SqlDecimal PreviousEventReadOrder { get; }
            public SqlDecimal NextEventReadOrder { get; }

            public EventOrderNeighborhood(long insertionOrder, SqlDecimal effectiveReadOrder, SqlDecimal previousEventReadOrder, SqlDecimal nextEventReadOrder)
            {
                InsertionOrder = insertionOrder;
                EffectiveReadOrder = effectiveReadOrder;
                NextEventReadOrder = UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(nextEventReadOrder);
                PreviousEventReadOrder = UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(previousEventReadOrder);
            }

            static SqlDecimal UseZeroInsteadIfNegativeSinceThisMeansThisIsTheFirstEventInTheEventStore(SqlDecimal previousReadOrder) => previousReadOrder > 0 ? previousReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(0));

            SqlDecimal UseNextIntegerInsteadIfNullSinceThatMeansThisEventIsTheLastInTheEventStore(SqlDecimal nextReadOrder) => !nextReadOrder.IsNull ? nextReadOrder : ToCorrectPrecisionAndScale(new SqlDecimal(InsertionOrder + 1));
        }

        EventOrderNeighborhood LoadEventInsertedBeforeAndAfter(Guid insertionOrder)
        {
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {SqlServerEventTable.Columns.InsertionOrder},
        {SqlServerEventTable.Columns.EffectiveReadOrder},        
        (select top 1 {SqlServerEventTable.Columns.EffectiveReadOrder} from {SqlServerEventTable.Name} e1 where e1.{SqlServerEventTable.Columns.EffectiveReadOrder} < {SqlServerEventTable.Name}.{SqlServerEventTable.Columns.EffectiveReadOrder} order by {SqlServerEventTable.Columns.EffectiveReadOrder} desc) PreviousReadOrder,
        (select top 1 {SqlServerEventTable.Columns.EffectiveReadOrder} from {SqlServerEventTable.Name} e1 where e1.{SqlServerEventTable.Columns.EffectiveReadOrder} > {SqlServerEventTable.Name}.{SqlServerEventTable.Columns.EffectiveReadOrder} order by {SqlServerEventTable.Columns.EffectiveReadOrder}) NextReadOrder
FROM    {SqlServerEventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {SqlServerEventTable.Columns.EventId} = @{SqlServerEventTable.Columns.EventId}";




            EventOrderNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;
                    command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.EventId, SqlDbType.UniqueIdentifier) {Value = insertionOrder});
                    using var reader = command.ExecuteReader();
                    reader.Read();

                    neighborhood = new EventOrderNeighborhood(
                        insertionOrder: reader.GetInt64(0),
                        effectiveReadOrder: reader.GetSqlDecimal(1),
                        previousEventReadOrder: reader.GetSqlDecimal(2),
                        nextEventReadOrder: reader.GetSqlDecimal(3));
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
                    command.CommandText +=
                        $"DELETE {SqlServerEventTable.Name} With(ROWLOCK) WHERE {SqlServerEventTable.Columns.AggregateId} = @{SqlServerEventTable.Columns.AggregateId}";
                    command.Parameters.Add(new SqlParameter(SqlServerEventTable.Columns.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }
    }
}
