using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System;
using Col = Composable.Persistence.SqlServer.EventStore.SqlServerEventTable.Columns;
using ReadOrder = Composable.Persistence.EventStore.IEventStorePersistenceLayer.ReadOrder;

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
(       {Col.AggregateId},  {Col.InsertedVersion},  {Col.EffectiveVersion},  {Col.EffectiveOrder},  {Col.EventType},  {Col.EventId},  {Col.UtcTimeStamp},  {Col.Event},  {Col.InsertAfter}, {Col.InsertBefore},  {Col.Replaces}) 
VALUES(@{Col.AggregateId}, @{Col.InsertedVersion}, @{Col.EffectiveVersion}, @{Col.EffectiveOrder}, @{Col.EventType}, @{Col.EventId}, @{Col.UtcTimeStamp}, @{Col.Event}, @{Col.InsertAfter},@{Col.InsertBefore}, @{Col.Replaces})
IF(@{Col.EffectiveOrder} IS NULL)
BEGIN
    UPDATE {SqlServerEventTable.Name} With(READCOMMITTED, ROWLOCK)
    SET {Col.EffectiveOrder} = cast({Col.InsertionOrder} as {SqlServerEventTable.ReadOrderType}),
        {Col.ReadOrder} = {Col.InsertionOrder}
    WHERE {Col.EventId} = @{Col.EventId}
END
")
                                           //SET @{Col.InsertionOrder} = SCOPE_IDENTITY();
                                          .AddParameter(Col.AggregateId, SqlDbType.UniqueIdentifier, data.AggregateId)
                                          .AddParameter(Col.InsertedVersion, data.RefactoringInformation.InsertedVersion)
                                          .AddParameter(Col.EventType, data.EventType)
                                          .AddParameter(Col.EventId, data.EventId)
                                          .AddDateTime2Parameter(Col.UtcTimeStamp, data.UtcTimeStamp)
                                          .AddNVarcharMaxParameter(Col.Event, data.EventJson)

                                          .AddNullableParameter(Col.EffectiveOrder, SqlDbType.Decimal, data.RefactoringInformation.EffectiveOrder?.ToSqlDecimal())
                                          .AddNullableParameter(Col.EffectiveVersion, SqlDbType.Int, data.RefactoringInformation.EffectiveVersion)
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

        public void UpdateEffectiveVersions(IReadOnlyList<IEventStorePersistenceLayer.ManualVersionSpecification> versions)
        {
            var commandText = versions.Select((spec, index) =>
                                                  $@"UPDATE {SqlServerEventTable.Name} SET {Col.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Col.EventId} = '{spec.EventId}'").Join(Environment.NewLine);

            _connectionManager.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        }

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {Col.EffectiveOrder},        
        (select top 1 {Col.EffectiveOrder} from {SqlServerEventTable.Name} e1 where e1.{Col.EffectiveOrder} < {SqlServerEventTable.Name}.{Col.EffectiveOrder} order by {Col.EffectiveOrder} desc) PreviousReadOrder,
        (select top 1 {Col.EffectiveOrder} from {SqlServerEventTable.Name} e1 where e1.{Col.EffectiveOrder} > {SqlServerEventTable.Name}.{Col.EffectiveOrder} order by {Col.EffectiveOrder}) NextReadOrder
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

                    var effectiveReadOrder = reader.GetSqlDecimal(0);
                    var previousEventReadOrder = reader.GetSqlDecimal(1);
                    var nextEventReadOrder = reader.GetSqlDecimal(2);
                    neighborhood = new IEventStorePersistenceLayer.EventNeighborhood(effectiveReadOrder: ReadOrder.FromSqlDecimal(effectiveReadOrder),
                                                                                     previousEventReadOrder: previousEventReadOrder.IsNull ? null : new ReadOrder?(ReadOrder.FromSqlDecimal(previousEventReadOrder)),
                                                                                     nextEventReadOrder: nextEventReadOrder.IsNull ? null : new ReadOrder?(ReadOrder.FromSqlDecimal(nextEventReadOrder)));
                });

            return Assert.Result.NotNull(neighborhood);
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
