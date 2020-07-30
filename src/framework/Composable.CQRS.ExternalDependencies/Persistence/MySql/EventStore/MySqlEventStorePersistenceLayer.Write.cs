using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.SystemCE;
using MySql.Data.MySqlClient;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.MySql.EventStore
{
    //Performance: explore MySql alternatives to commented out MSSql hints throughout the persistence layer.
    partial class MySqlEventStorePersistenceLayer
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
INSERT {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
(       {Event.AggregateId},  {Event.InsertedVersion},  {Event.EffectiveVersion},  {Event.ReadOrder},  {Event.EventType},  {Event.EventId},  {Event.UtcTimeStamp},  {Event.Event},  {Event.TargetEvent}, {Event.RefactoringType}) 
VALUES(@{Event.AggregateId}, @{Event.InsertedVersion}, @{Event.EffectiveVersion}, @{Event.ReadOrder}, @{Event.EventType}, @{Event.EventId}, @{Event.UtcTimeStamp}, @{Event.Event}, @{Event.TargetEvent},@{Event.RefactoringType});


IF @{Event.ReadOrder} = '0.0000000000000000000' THEN

    UPDATE {Event.TableName} /*With(READCOMMITTED, ROWLOCK)*/
    SET {Event.ReadOrder} = cast({Event.InsertionOrder} as {Event.ReadOrderType})
    WHERE {Event.EventId} = @{Event.EventId};
END IF;
")
                                              .AddParameter(Event.AggregateId, data.AggregateId)
                                              .AddParameter(Event.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(Event.EventType, data.EventType)
                                              .AddParameter(Event.EventId, data.EventId)
                                              .AddDateTime2Parameter(Event.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddMediumTextParameter(Event.Event, data.EventJson)

                                              .AddParameter(Event.ReadOrder, MySqlDbType.VarChar, data.StorageInformation.ReadOrder?.ToString() ?? ReadOrder.Zero.ToString())
                                              .AddParameter(Event.EffectiveVersion, MySqlDbType.Int32, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(Event.TargetEvent, MySqlDbType.VarChar, data.StorageInformation.RefactoringInformation?.TargetEvent)
                                              .AddNullableParameter(Event.RefactoringType, MySqlDbType.Byte, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(MySqlException e) when (SqlExceptions.MySql.IsPrimaryKeyViolation(e))
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
                                                  $@"UPDATE {Event.TableName} SET {Event.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Event.EventId} = '{spec.EventId}';").Join(Environment.NewLine);

            _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

        }

        public EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            //var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

            var selectStatement = $@"
SELECT  {Event.ReadOrder},        
        (select cast({Event.ReadOrder} as char(39)) from {Event.TableName} e1 where e1.{Event.ReadOrder} < {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder} desc limit 1) PreviousReadOrder,
        (select cast({Event.ReadOrder} as char(39)) from {Event.TableName} e1 where e1.{Event.ReadOrder} > {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder} limit 1) NextReadOrder
FROM    {Event.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {Event.EventId} = @{Event.EventId}";

            EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;

                    command.Parameters.Add(new MySqlParameter(Event.EventId, MySqlDbType.Guid) { Value = eventId });
                    using var reader = command.ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = reader.GetString(0).ReplaceInvariant(",", ".");
                    var previousEventReadOrder = (reader[1] as string)?.ReplaceInvariant(",", ".");
                    var nextEventReadOrder = (reader[2] as string)?.ReplaceInvariant(",", ".");
                    neighborhood = new EventNeighborhood(effectiveReadOrder: ReadOrder.Parse(effectiveReadOrder),
                                                         previousEventReadOrder: previousEventReadOrder == null ? null : new ReadOrder?(ReadOrder.Parse(previousEventReadOrder)),
                                                         nextEventReadOrder: nextEventReadOrder == null ? null : new ReadOrder?(ReadOrder.Parse(nextEventReadOrder)));
                });

            return Assert.Result.NotNull(neighborhood);
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText +=
                        $"DELETE FROM {Event.TableName} /*With(ROWLOCK)*/ WHERE {Event.AggregateId} = @{Event.AggregateId};";
                    command.Parameters.Add(new MySqlParameter(Event.AggregateId, MySqlDbType.Guid) { Value = aggregateId });
                    command.ExecuteNonQuery();
                });
        }
    }
}
