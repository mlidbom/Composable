using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.System;
using IBM.Data.DB2.Core;
using IBM.Data.DB2Types;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.DB2.EventStore
{
    partial class DB2EventStorePersistenceLayer
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
                                                   //urgent: explore db2 alternatives to commented out hints .
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
                                              .AddParameter(Event.ReadOrder, DB2Type.Decimal, (data.StorageInformation.ReadOrder?.ToDB2Decimal() ?? new DB2Decimal(0)))
                                              .AddParameter(Event.EffectiveVersion, DB2Type.Integer, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(Event.TargetEvent, DB2Type.VarChar, data.StorageInformation.RefactoringInformation?.TargetEvent)
                                              .AddNullableParameter(Event.RefactoringType, DB2Type.Byte, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(DB2Exception e) when ((e.Data["Server Error Code"] as int?)  == PrimaryKeyViolationSqlErrorNumber )
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
            //urgent: Find DB2 equivalent
            //var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

            //Urgent: Using min and max instead of order by and top is far more clean. Do the same in the other persistence layer.s
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
                                             .AddParameter(new DB2Parameter(Event.EventId, DB2Type.VarChar) { Value = eventId })
                                              .ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = reader.GetDB2Decimal(0);
                    var previousEventReadOrder = reader[1] == DBNull.Value ? (DB2Decimal?)null! : reader.GetDB2Decimal(1);
                    var nextEventReadOrder = reader[2] == DBNull.Value ? (DB2Decimal?)null! : reader.GetDB2Decimal(2);
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
                        $"DELETE FROM {Event.TableName} /*With(ROWLOCK)*/ WHERE {Event.AggregateId} = :{Event.AggregateId}";
                    command.Parameters.Add(new DB2Parameter(Event.AggregateId, DB2Type.VarChar) { Value = aggregateId });
                    command.ExecuteNonQuery();
                });
        }
    }
}
