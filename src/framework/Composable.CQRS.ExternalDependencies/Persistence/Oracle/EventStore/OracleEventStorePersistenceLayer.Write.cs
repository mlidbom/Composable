using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.System;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using C = Composable.Persistence.Common.EventStore.EventTable.Columns;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;

namespace Composable.Persistence.Oracle.EventStore
{
    partial class OracleEventStorePersistenceLayer
    {
        const int PrimaryKeyViolationSqlErrorNumber = 1062;
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
                                                   //urgent: explore oracle alternatives to commented out hints .
                                                   $@"
BEGIN
INSERT INTO {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
(       {C.AggregateId},  {C.InsertedVersion},  {C.EffectiveVersion},  {C.EffectiveOrder},  {C.EventType},  {C.EventId},  {C.UtcTimeStamp},  {C.Event},  {C.TargetEvent}, {C.RefactoringType}) 
VALUES(:{C.AggregateId}, :{C.InsertedVersion}, :{C.EffectiveVersion}, :{C.EffectiveOrder}, :{C.EventType}, :{C.EventId}, :{C.UtcTimeStamp}, :{C.Event}, :{C.TargetEvent},:{C.RefactoringType});

{(data.StorageInformation.ReadOrder != null ? "":$@"

UPDATE {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
        SET {C.EffectiveOrder} = cast({C.InsertionOrder} as {EventTable.ReadOrderType}),
            {C.ReadOrder} = {C.InsertionOrder}
        WHERE {C.EventId} = :{C.EventId};
")}

END;
")
                                              .AddParameter(C.AggregateId, data.AggregateId)
                                              .AddParameter(C.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(C.EventType, data.EventType)
                                              .AddParameter(C.EventId, data.EventId)
                                              .AddParameter(C.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddNClobParameter(C.Event, data.EventJson)

                                              .AddNullableParameter(C.EffectiveOrder, OracleDbType.Decimal, data.StorageInformation.ReadOrder?.ToOracleDecimal())
                                              .AddNullableParameter(C.EffectiveVersion, OracleDbType.Int32, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(C.TargetEvent, OracleDbType.Varchar2, data.StorageInformation.RefactoringInformation?.TargetEvent)
                                              .AddNullableParameter(C.RefactoringType, OracleDbType.Byte, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(OracleException e) when ((e.Data["Server Error Code"] as int?)  == PrimaryKeyViolationSqlErrorNumber )
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
                                                  $@"UPDATE {EventTable.Name} SET {C.EffectiveVersion} = {spec.EffectiveVersion} WHERE {C.EventId} = '{spec.EventId}';").Join(Environment.NewLine);

            _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

        }

        public EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            //urgent: Find Oracle equivalent
            //var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

            var selectStatement = $@"
SELECT  {C.EffectiveOrder},        
        (select {C.EffectiveOrder} from {EventTable.Name} e1 where ROWNUM <= 1 AND e1.{C.EffectiveOrder} < {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} desc) PreviousReadOrder,
        (select {C.EffectiveOrder} from {EventTable.Name} e1 where ROWNUM <= 1 AND e1.{C.EffectiveOrder} > {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} ) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {C.EventId} = :{C.EventId}";

            EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;

                    command.Parameters.Add(new OracleParameter(C.EventId, OracleDbType.Varchar2) { Value = eventId });
                    using var reader = command.ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = reader.GetOracleDecimal(0);
                    var previousEventReadOrder = reader[1] as OracleDecimal?;
                    var nextEventReadOrder = reader[2] as OracleDecimal?;
                    neighborhood = new EventNeighborhood(effectiveReadOrder: effectiveReadOrder.ToReadOrder(),
                                                         previousEventReadOrder: previousEventReadOrder?.ToReadOrder(),
                                                         nextEventReadOrder: nextEventReadOrder?.ToReadOrder());
                });

            return Assert.Result.NotNull(neighborhood);
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            _connectionManager.UseCommand(
                command =>
                {
                    //urgent: Find equivalent to rowlock hint.
                    command.CommandText +=
                        $"DELETE FROM {EventTable.Name} /*With(ROWLOCK)*/ WHERE {C.AggregateId} = :{C.AggregateId}";
                    command.Parameters.Add(new OracleParameter(C.AggregateId, OracleDbType.Varchar2) { Value = aggregateId });
                    command.ExecuteNonQuery();
                });
        }
    }
}
