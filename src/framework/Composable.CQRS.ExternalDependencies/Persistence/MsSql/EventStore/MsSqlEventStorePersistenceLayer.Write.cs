using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.System;
using C = Composable.Persistence.Common.EventStore.EventTable.Columns;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;

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
(       {C.AggregateId},  {C.InsertedVersion},  {C.EffectiveVersion},  {C.ReadOrder},  {C.EventType},  {C.EventId},  {C.UtcTimeStamp},  {C.Event},  {C.TargetEvent}, {C.RefactoringType}) 
VALUES(@{C.AggregateId}, @{C.InsertedVersion}, @{C.EffectiveVersion}, @{C.ReadOrder}, @{C.EventType}, @{C.EventId}, @{C.UtcTimeStamp}, @{C.Event}, @{C.TargetEvent},@{C.RefactoringType})


IF(@{C.ReadOrder} = 0)
BEGIN
    UPDATE {EventTable.Name} With(READCOMMITTED, ROWLOCK)
    SET {C.ReadOrder} = cast({C.InsertionOrder} as {EventTable.ReadOrderType})
    WHERE {C.EventId} = @{C.EventId}
END
")
                                              .AddParameter(C.AggregateId, SqlDbType.UniqueIdentifier, data.AggregateId)
                                              .AddParameter(C.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(C.EventType, data.EventType)
                                              .AddParameter(C.EventId, data.EventId)
                                              .AddDateTime2Parameter(C.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddNVarcharMaxParameter(C.Event, data.EventJson)

                                              .AddParameter(C.ReadOrder, SqlDbType.Decimal, data.StorageInformation.ReadOrder?.ToSqlDecimal() ?? new SqlDecimal(0))
                                              .AddParameter(C.EffectiveVersion, SqlDbType.Int, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(C.TargetEvent, SqlDbType.UniqueIdentifier, data.StorageInformation.RefactoringInformation?.TargetEvent)
                                              .AddNullableParameter(C.RefactoringType, SqlDbType.TinyInt, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(SqlException e) when(e.Number == PrimaryKeyViolationSqlErrorNumber)
                    {
                        //todo: Make sure we have test coverage for this.
                        throw new EventDuplicateKeyException(e);
                    }
                }
            });
        }

        public void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions)
        {
            var commandText = versions.Select((spec, index) =>
                                                  $@"UPDATE {EventTable.Name} SET {C.EffectiveVersion} = {spec.EffectiveVersion} WHERE {C.EventId} = '{spec.EventId}'").Join(Environment.NewLine);

            _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

        }

        public EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {


            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {C.ReadOrder},        
        (select top 1 {C.ReadOrder} from {EventTable.Name} e1 where e1.{C.ReadOrder} < {EventTable.Name}.{C.ReadOrder} order by {C.ReadOrder} desc) PreviousReadOrder,
        (select top 1 {C.ReadOrder} from {EventTable.Name} e1 where e1.{C.ReadOrder} > {EventTable.Name}.{C.ReadOrder} order by {C.ReadOrder}) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {C.EventId} = @{C.EventId}";

            EventNeighborhood? neighborhood = null;

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
                    neighborhood = new EventNeighborhood(effectiveReadOrder: ReadOrder.FromSqlDecimal(effectiveReadOrder),
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
