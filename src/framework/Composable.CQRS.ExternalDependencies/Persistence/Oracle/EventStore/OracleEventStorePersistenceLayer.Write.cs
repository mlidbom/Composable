using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.SystemCE;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.Oracle.EventStore
{
    //Performance: explore Oracle alternatives to commented out MSSql hints throughout the persistence layer.
    partial class OracleEventStorePersistenceLayer
    {
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
                                                   $@"
BEGIN
    IF (:{Event.InsertedVersion} = 1) THEN
        insert into {Lock.TableName}({Lock.AggregateId}) values(:{Lock.AggregateId});
    END IF;

    INSERT INTO {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
    (       {Event.AggregateId},  {Event.InsertedVersion},  {Event.EffectiveVersion},  {Event.ReadOrder},  {Event.EventType},  {Event.EventId},  {Event.UtcTimeStamp},  {Event.Event},  {Event.TargetEvent}, {Event.RefactoringType}) 
    VALUES(:{Event.AggregateId}, :{Event.InsertedVersion}, :{Event.EffectiveVersion}, :{Event.ReadOrder}, :{Event.EventType}, :{Event.EventId}, :{Event.UtcTimeStamp}, :{Event.Event}, :{Event.TargetEvent},:{Event.RefactoringType});


    IF (:{Event.ReadOrder} = 0) THEN
        UPDATE {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                SET {Event.ReadOrder} = cast({Event.InsertionOrder} as {Event.ReadOrderType})
                WHERE {Event.EventId} = :{Event.EventId};
    END IF;
END;
")
                                              .AddParameter(Event.AggregateId, data.AggregateId)
                                              .AddParameter(Event.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(Event.EventType, data.EventType)
                                              .AddParameter(Event.EventId, data.EventId)
                                              .AddParameter(Event.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddNClobParameter(Event.Event, data.EventJson)
                                              .AddParameter(Event.ReadOrder, OracleDbType.Decimal, (data.StorageInformation.ReadOrder?.ToOracleDecimal() ?? new OracleDecimal(0)))
                                              .AddParameter(Event.EffectiveVersion, OracleDbType.Int32, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(Event.TargetEvent, OracleDbType.Varchar2, data.StorageInformation.RefactoringInformation?.TargetEvent)
                                              .AddNullableParameter(Event.RefactoringType, OracleDbType.Byte, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(OracleException e) when (SqlExceptions.Oracle.IsPrimaryKeyViolation_TODO(e))
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
                     $@"UPDATE {Event.TableName} SET {Event.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Event.EventId} = '{spec.EventId}';").Join(Environment.NewLine)}

END;";
            _connectionManager.UseCommand(command => command.SetCommandText(commandText).ExecuteNonQuery());

        }

        public EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            //var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

            //Urgent: Using min and max instead of order by and top is far more clean. Do the same in the other persistence layers
            var selectStatement = $@"
SELECT  {Event.ReadOrder},        
        (select MAX({Event.ReadOrder}) from {Event.TableName} e1 where e1.{Event.ReadOrder} < {Event.TableName}.{Event.ReadOrder}) PreviousReadOrder,
        (select MIN({Event.ReadOrder}) from {Event.TableName} e1 where e1.{Event.ReadOrder} > {Event.TableName}.{Event.ReadOrder}) NextReadOrder
FROM    {Event.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {Event.EventId} = :{Event.EventId}";

            EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    using var reader = command.SetCommandText(selectStatement)
                                             .AddParameter(new OracleParameter(Event.EventId, OracleDbType.Varchar2) { Value = eventId })
                                              .ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = reader.GetOracleDecimal(0);
                    var previousEventReadOrder = reader[1] == DBNull.Value ? (OracleDecimal?)null : reader.GetOracleDecimal(1);
                    var nextEventReadOrder = reader[2] == DBNull.Value ? (OracleDecimal?)null : reader.GetOracleDecimal(2);
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
                    command.CommandText +=
                        $"DELETE FROM {Event.TableName} /*With(ROWLOCK)*/ WHERE {Event.AggregateId} = :{Event.AggregateId}";
                    command.Parameters.Add(new OracleParameter(Event.AggregateId, OracleDbType.Varchar2) { Value = aggregateId });
                    command.ExecuteNonQuery();
                });
        }
    }
}
