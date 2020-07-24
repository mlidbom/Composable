using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.SystemCE;
using IBM.Data.DB2.Core;
using IBM.Data.DB2Types;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.DB2.EventStore
{
    partial class DB2EventStorePersistenceLayer
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
                                                   //urgent: explore db2 alternatives to commented out hints .
                                                   $@"
BEGIN
    IF (@{Event.InsertedVersion} = 1) THEN
        insert into {Lock.TableName}({Lock.AggregateId}) values(@{Lock.AggregateId});
    END IF;

    INSERT INTO {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
    (       {Event.AggregateId},  {Event.InsertedVersion},  {Event.EffectiveVersion},  {Event.ReadOrderIntegerPart},  {Event.ReadOrderFractionPart},  {Event.EventType},  {Event.EventId},  {Event.UtcTimeStamp},  {Event.Event},  {Event.TargetEvent}, {Event.RefactoringType}) 
    VALUES(@{Event.AggregateId}, @{Event.InsertedVersion}, @{Event.EffectiveVersion}, @{Event.ReadOrderIntegerPart}, @{Event.ReadOrderFractionPart}, @{Event.EventType}, @{Event.EventId}, @{Event.UtcTimeStamp}, @{Event.Event}, @{Event.TargetEvent},@{Event.RefactoringType});


    IF (@{Event.ReadOrderIntegerPart} = 0) THEN
        UPDATE {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
                SET {Event.ReadOrderIntegerPart} = cast({Event.InsertionOrder} as DECIMAL(19))
                WHERE {Event.EventId} = @{Event.EventId};
    END IF;
END;
")
                                              .AddParameter(Event.AggregateId, data.AggregateId)
                                              .AddParameter(Event.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(Event.EventType, data.EventType)
                                              .AddParameter(Event.EventId, data.EventId)
                                              .AddParameter(Event.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddNClobParameter(Event.Event, data.EventJson)
                                              .AddParameter(Event.ReadOrderIntegerPart, DB2Type.Decimal, (data.StorageInformation.ReadOrder?.ToDB2DecimalIntegerPart() ?? new DB2Decimal(0)))
                                              .AddParameter(Event.ReadOrderFractionPart, DB2Type.Decimal, (data.StorageInformation.ReadOrder?.ToDB2DecimalFractionPart() ?? new DB2Decimal(0)))
                                              .AddParameter(Event.EffectiveVersion, DB2Type.Integer, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(Event.TargetEvent, DB2Type.VarChar, data.StorageInformation.RefactoringInformation?.TargetEvent.ToString())
                                              .AddNullableParameter(Event.RefactoringType, DB2Type.SmallInt, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (int?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(DB2Exception e) when (SqlExceptions.DB2.IsPrimaryKeyViolation(e))
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

            var selectStatement = $@"
SELECT  {Event.ReadOrderIntegerPart} CONCAT '.' CONCAT {Event.ReadOrderFractionPart},

        (select {Event.ReadOrderIntegerPart} CONCAT '.' CONCAT {Event.ReadOrderFractionPart} from {Event.TableName} e1 
            where e1.{Event.ReadOrderIntegerPart} < {Event.TableName}.{Event.ReadOrderIntegerPart} 
                    OR (    e1.{Event.ReadOrderIntegerPart} = {Event.TableName}.{Event.ReadOrderIntegerPart} 
                        AND e1.{Event.ReadOrderFractionPart} < {Event.TableName}.{Event.ReadOrderFractionPart})
            ORDER BY e1.{Event.ReadOrderIntegerPart} DESC, e1.{Event.ReadOrderIntegerPart} DESC
            FETCH FIRST 1 ROWS ONLY) PreviousReadOrder,

        (select {Event.ReadOrderIntegerPart} CONCAT '.' CONCAT {Event.ReadOrderFractionPart} from {Event.TableName} e1 
            where e1.{Event.ReadOrderIntegerPart} > {Event.TableName}.{Event.ReadOrderIntegerPart} 
                    OR (    e1.{Event.ReadOrderIntegerPart} = {Event.TableName}.{Event.ReadOrderIntegerPart} 
                        AND e1.{Event.ReadOrderFractionPart} > {Event.TableName}.{Event.ReadOrderFractionPart}) 
            ORDER BY e1.{Event.ReadOrderIntegerPart} ASC, e1.{Event.ReadOrderIntegerPart} ASC
            FETCH FIRST 1 ROWS ONLY) NextReadOrder
FROM    {Event.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {Event.EventId} = @{Event.EventId}";

            EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    using var reader = command.SetCommandText(selectStatement)
                                             .AddParameter(Event.EventId, eventId)
                                              .ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = ReadOrder.Parse(reader.GetString(0), bypassScaleTest: true);
                    var previousEventReadOrder = reader[1] is string prevString ? ReadOrder.Parse(prevString, bypassScaleTest: true): (ReadOrder?)null;
                    var nextEventReadOrder = reader[2] is string nextString ? ReadOrder.Parse(nextString, bypassScaleTest: true): (ReadOrder?)null;
                    neighborhood = new EventNeighborhood(effectiveReadOrder: effectiveReadOrder,
                                                         previousEventReadOrder: previousEventReadOrder,
                                                         nextEventReadOrder: nextEventReadOrder);
                });

            return Assert.Result.NotNull(neighborhood);
        }

        public void DeleteAggregate(Guid aggregateId) =>
            _connectionManager.UseCommand(
                command =>
                {
                    //urgent: Find equivalent to rowlock hint.
                    command.SetCommandText($"DELETE FROM {Event.TableName} /*With(ROWLOCK)*/ WHERE {Event.AggregateId} = @{Event.AggregateId}")
                           .AddParameter(Event.AggregateId, aggregateId)
                           .ExecuteNonQuery();
                });
    }
}
