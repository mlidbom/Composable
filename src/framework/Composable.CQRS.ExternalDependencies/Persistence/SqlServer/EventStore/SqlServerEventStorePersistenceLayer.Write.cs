using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Composable.Contracts;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;
using Col = Composable.Persistence.SqlServer.EventStore.SqlServerEventTable.Columns;

namespace Composable.Persistence.SqlServer.EventStore
{
    partial class SqlServerEventStorePersistenceLayer
    {
        const int PrimaryKeyViolationSqlErrorNumber = 2627;

        public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events)
        {
            foreach(var data in events)
            {
                try
                {
                    _connectionManager.UseCommand(
                        command => command.SetCommandText(
                                               //urgent: ensure that READCOMMITTED is really sane here and add comment.
                                               $@"
INSERT {SqlServerEventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {Col.AggregateId},  {Col.InsertedVersion},  {Col.ManualVersion},  {Col.ManualReadOrder},  {Col.EventType},  {Col.EventId},  {Col.UtcTimeStamp},  {Col.Event},  {Col.InsertAfter}, {Col.InsertBefore},  {Col.Replaces}) 
VALUES(@{Col.AggregateId}, @{Col.InsertedVersion}, @{Col.ManualVersion}, @{Col.ManualReadOrder}, @{Col.EventType}, @{Col.EventId}, @{Col.UtcTimeStamp}, @{Col.Event}, @{Col.InsertAfter},@{Col.InsertBefore}, @{Col.Replaces})")
                                          .AddParameter(Col.AggregateId, SqlDbType.UniqueIdentifier, data.AggregateId)
                                          .AddParameter(Col.InsertedVersion, data.RefactoringInformation.InsertedVersion)
                                          .AddParameter(Col.EventType, data.EventType)
                                          .AddParameter(Col.EventId, data.EventId)
                                          .AddDateTime2Parameter(Col.UtcTimeStamp, data.UtcTimeStamp)
                                          .AddNVarcharMaxParameter(Col.Event, data.EventJson)

                                          .AddNullableParameter(Col.ManualReadOrder, SqlDbType.Decimal, data.RefactoringInformation.ManualReadOrder)
                                          .AddNullableParameter(Col.ManualVersion, SqlDbType.Int, data.RefactoringInformation.ManualVersion)
                                          .AddNullableParameter(Col.InsertAfter, SqlDbType.UniqueIdentifier, data.RefactoringInformation.InsertAfter)
                                          .AddNullableParameter(Col.InsertBefore, SqlDbType.UniqueIdentifier, data.RefactoringInformation.InsertBefore)
                                          .AddNullableParameter(Col.Replaces, SqlDbType.UniqueIdentifier, data.RefactoringInformation.Replaces)
                                          .ExecuteNonQuery());
                }
                catch(SqlException e) when(e.Number == PrimaryKeyViolationSqlErrorNumber)
                {
                    //todo: Make sure we have test coverage for this.
                    throw new SqlServerEventStoreOptimisticConcurrencyException(e);
                }
            }
        }

        //Urgent: Do this logic in C# in the EventStore class. Persistence layer should only save the data, not implement logic that can be common for all persistence layers.
        public void FixManualVersions(Guid aggregateId)
        {
            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = SqlServerEventStore.SqlStatements.FixManualVersionsForAggregate;
                    command.Parameters.Add(new SqlParameter(Col.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {Col.InsertionOrder},
        {Col.EffectiveReadOrder},        
        (select top 1 {Col.EffectiveReadOrder} from {SqlServerEventTable.Name} e1 where e1.{Col.EffectiveReadOrder} < {SqlServerEventTable.Name}.{Col.EffectiveReadOrder} order by {Col.EffectiveReadOrder} desc) PreviousReadOrder,
        (select top 1 {Col.EffectiveReadOrder} from {SqlServerEventTable.Name} e1 where e1.{Col.EffectiveReadOrder} > {SqlServerEventTable.Name}.{Col.EffectiveReadOrder} order by {Col.EffectiveReadOrder}) NextReadOrder
FROM    {SqlServerEventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {Col.EventId} = @{Col.EventId}";

            IEventStorePersistenceLayer.EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;
                    command.Parameters.Add(new SqlParameter(Col.EventId, SqlDbType.UniqueIdentifier) {Value = eventId});
                    using var reader = command.ExecuteReader();
                    reader.Read();

                    neighborhood = new IEventStorePersistenceLayer.EventNeighborhood(
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
                        $"DELETE {SqlServerEventTable.Name} With(ROWLOCK) WHERE {Col.AggregateId} = @{Col.AggregateId}";
                    command.Parameters.Add(new SqlParameter(Col.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }
    }
}
