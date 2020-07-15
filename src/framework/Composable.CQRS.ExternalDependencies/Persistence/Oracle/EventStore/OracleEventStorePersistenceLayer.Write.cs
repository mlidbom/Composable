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
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTable;

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
    IF (:{C.InsertedVersion} = 1) THEN
        insert into {Lock.TableName}({Lock.AggregateId}) values(:{Lock.AggregateId});
    END IF;

    INSERT INTO {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
    (       {C.AggregateId},  {C.InsertedVersion},  {C.EffectiveVersion},  {C.ReadOrder},  {C.EventType},  {C.EventId},  {C.UtcTimeStamp},  {C.Event},  {C.TargetEvent}, {C.RefactoringType}) 
    VALUES(:{C.AggregateId}, :{C.InsertedVersion}, :{C.EffectiveVersion}, :{C.ReadOrder}, :{C.EventType}, :{C.EventId}, :{C.UtcTimeStamp}, :{C.Event}, :{C.TargetEvent},:{C.RefactoringType});


    IF (:{C.ReadOrder} = 0) THEN
        UPDATE {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
                SET {C.ReadOrder} = cast({C.InsertionOrder} as {EventTable.ReadOrderType})
                WHERE {C.EventId} = :{C.EventId};
    END IF;
END;
")
                                              .AddParameter(C.AggregateId, data.AggregateId)
                                              .AddParameter(C.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(C.EventType, data.EventType)
                                              .AddParameter(C.EventId, data.EventId)
                                              .AddParameter(C.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddNClobParameter(C.Event, data.EventJson)
                                              .AddParameter(C.ReadOrder, OracleDbType.Decimal, (data.StorageInformation.ReadOrder?.ToOracleDecimal() ?? new OracleDecimal(0)))
                                              .AddParameter(C.EffectiveVersion, OracleDbType.Int32, data.StorageInformation.EffectiveVersion)
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
            var commandText = $@"
BEGIN

{versions.Select((spec, index) =>
                     $@"UPDATE {EventTable.Name} SET {C.EffectiveVersion} = {spec.EffectiveVersion} WHERE {C.EventId} = '{spec.EventId}';").Join(Environment.NewLine)}

END;";
            _connectionManager.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        }

        public EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            //urgent: Find Oracle equivalent
            //var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

            //Urgent: Using min and max instead of order by and top is far more clean. Do the same in the other persistence layer.s
            var selectStatement = $@"
SELECT  {C.ReadOrder},        
        (select MAX({C.ReadOrder}) from {EventTable.Name} e1 where e1.{C.ReadOrder} < {EventTable.Name}.{C.ReadOrder}) PreviousReadOrder,
        (select MIN({C.ReadOrder}) from {EventTable.Name} e1 where e1.{C.ReadOrder} > {EventTable.Name}.{C.ReadOrder}) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {C.EventId} = :{C.EventId}";

            EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    using var reader = command.SetCommandText(selectStatement)
                                             .AddParameter(new OracleParameter(C.EventId, OracleDbType.Varchar2) { Value = eventId })
                                              .ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = reader.GetOracleDecimal(0);
                    var previousEventReadOrder = reader[1] == DBNull.Value ? (OracleDecimal?)null! : reader.GetOracleDecimal(1);
                    var nextEventReadOrder = reader[2] == DBNull.Value ? (OracleDecimal?)null! : reader.GetOracleDecimal(2);
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
