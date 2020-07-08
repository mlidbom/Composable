using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.System;
using C = Composable.Persistence.Common.EventStore.EventTable.Columns;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.IEventStorePersistenceLayer.ReadOrder;

namespace Composable.Persistence.MsSql.EventStore
{
    partial class MsSqlEventStorePersistenceLayer
    {
        const int PrimaryKeyViolationSqlErrorNumber = 2627;

        public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events)
        {
            _connectionManager.UseConnection(connection =>
            {
                foreach(var data in events)
                {
                    try
                    {
                        connection.UseCommand(
                            command => command.SetCommandText(
                                                   //urgent: ensure that READCOMMITTED is really sane here and add comment.
                                                   $@"
INSERT {EventTable.Name} With(READCOMMITTED, ROWLOCK) 
(       {C.AggregateId},  {C.InsertedVersion},  {C.EffectiveVersion},  {C.EffectiveOrder},  {C.EventType},  {C.EventId},  {C.UtcTimeStamp},  {C.Event},  {C.TargetEvent}, {C.RefactoringType}) 
VALUES(@{C.AggregateId}, @{C.InsertedVersion}, @{C.EffectiveVersion}, @{C.EffectiveOrder}, @{C.EventType}, @{C.EventId}, @{C.UtcTimeStamp}, @{C.Event}, @{C.TargetEvent},@{C.RefactoringType})


IF(@{C.EffectiveOrder} IS NULL)
BEGIN
    UPDATE {EventTable.Name} With(READCOMMITTED, ROWLOCK)
    SET {C.EffectiveOrder} = cast({C.InsertionOrder} as {EventTable.ReadOrderType}),
        {C.ReadOrder} = {C.InsertionOrder}
    WHERE {C.EventId} = @{C.EventId}
END
")
                                              .AddParameter(C.AggregateId, SqlDbType.UniqueIdentifier, data.AggregateId)
                                              .AddParameter(C.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(C.EventType, data.EventType)
                                              .AddParameter(C.EventId, data.EventId)
                                              .AddDateTime2Parameter(C.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddNVarcharMaxParameter(C.Event, data.EventJson)

                                              .AddNullableParameter(C.EffectiveOrder, SqlDbType.Decimal, data.StorageInformation.ReadOrder?.ToSqlDecimal())
                                              .AddNullableParameter(C.EffectiveVersion, SqlDbType.Int, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(C.TargetEvent, SqlDbType.UniqueIdentifier, data.StorageInformation.RefactoringInformation?.TargetEvent)
                                              .AddNullableParameter(C.RefactoringType, SqlDbType.TinyInt, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(SqlException e) when(e.Number == PrimaryKeyViolationSqlErrorNumber)
                    {
                        //todo: Make sure we have test coverage for this.
                        throw new EventStoreOptimisticConcurrencyException(e);
                    }
                }
            });
        }

        public void UpdateEffectiveVersions(IReadOnlyList<IEventStorePersistenceLayer.ManualVersionSpecification> versions)
        {
            var commandText = versions.Select((spec, index) =>
                                                  $@"UPDATE {EventTable.Name} SET {C.EffectiveVersion} = {spec.EffectiveVersion} WHERE {C.EventId} = '{spec.EventId}'").Join(Environment.NewLine);

            _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

        }

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {


            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {C.EffectiveOrder},        
        (select top 1 {C.EffectiveOrder} from {EventTable.Name} e1 where e1.{C.EffectiveOrder} < {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} desc) PreviousReadOrder,
        (select top 1 {C.EffectiveOrder} from {EventTable.Name} e1 where e1.{C.EffectiveOrder} > {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder}) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {C.EventId} = @{C.EventId}";

            IEventStorePersistenceLayer.EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;
                    command.Parameters.Add(new SqlParameter(C.EventId, SqlDbType.UniqueIdentifier) {Value = eventId});
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
                        $"DELETE {EventTable.Name} With(ROWLOCK) WHERE {C.AggregateId} = @{C.AggregateId}";
                    command.Parameters.Add(new SqlParameter(C.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }
    }
}
