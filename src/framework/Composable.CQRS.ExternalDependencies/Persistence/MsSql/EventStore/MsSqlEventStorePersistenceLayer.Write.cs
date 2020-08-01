using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common;
using Composable.Persistence.Common.AdoCE;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.SystemCE;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;
using Event=Composable.Persistence.Common.EventStore.EventTableSchemaStrings;
using Lock = Composable.Persistence.Common.EventStore.AggregateLockTableSchemaStrings;

namespace Composable.Persistence.MsSql.EventStore
{
    partial class MsSqlEventStorePersistenceLayer
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
INSERT {Event.TableName} With(READCOMMITTED, ROWLOCK) --We are inserting append-only data so using READCOMMITTED to lessen the risk of lock problems should be perfectly safe.
(       {Event.AggregateId},  {Event.InsertedVersion},  {Event.EffectiveVersion},  {Event.ReadOrder},  {Event.EventType},  {Event.EventId},  {Event.UtcTimeStamp},  {Event.Event},  {Event.TargetEvent}, {Event.RefactoringType}) 
VALUES(@{Event.AggregateId}, @{Event.InsertedVersion}, @{Event.EffectiveVersion}, @{Event.ReadOrder}, @{Event.EventType}, @{Event.EventId}, @{Event.UtcTimeStamp}, @{Event.Event}, @{Event.TargetEvent},@{Event.RefactoringType})


IF(@{Event.ReadOrder} = 0)
BEGIN
    UPDATE {Event.TableName} With(READCOMMITTED, ROWLOCK)
    SET {Event.ReadOrder} = cast({Event.InsertionOrder} as {Event.ReadOrderType})
    WHERE {Event.EventId} = @{Event.EventId}
END
")
                                              .AddParameter(Event.AggregateId, SqlDbType.UniqueIdentifier, data.AggregateId)
                                              .AddParameter(Event.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(Event.EventType, data.EventType)
                                              .AddParameter(Event.EventId, data.EventId)
                                              .AddDateTime2Parameter(Event.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddNVarcharMaxParameter(Event.Event, data.EventJson)

                                              .AddParameter(Event.ReadOrder, SqlDbType.Decimal, data.StorageInformation.ReadOrder?.ToSqlDecimal() ?? new SqlDecimal(0))
                                              .AddParameter(Event.EffectiveVersion, SqlDbType.Int, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(Event.TargetEvent, SqlDbType.UniqueIdentifier, data.StorageInformation.RefactoringInformation?.TargetEvent)
                                              .AddNullableParameter(Event.RefactoringType, SqlDbType.TinyInt, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(SqlException e) when(SqlExceptions.MsSql.IsPrimaryKeyViolation(e))
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
                                                  $@"UPDATE {Event.TableName} SET {Event.EffectiveVersion} = {spec.EffectiveVersion} WHERE {Event.EventId} = '{spec.EventId}'").Join(Environment.NewLine);

            _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

        }

        public EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {


            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {Event.ReadOrder},        
        (select top 1 {Event.ReadOrder} from {Event.TableName} e1 where e1.{Event.ReadOrder} < {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder} desc) PreviousReadOrder,
        (select top 1 {Event.ReadOrder} from {Event.TableName} e1 where e1.{Event.ReadOrder} > {Event.TableName}.{Event.ReadOrder} order by {Event.ReadOrder}) NextReadOrder
FROM    {Event.TableName} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {Event.EventId} = @{Event.EventId}";

            EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;
                    command.Parameters.Add(new SqlParameter(Event.EventId, SqlDbType.UniqueIdentifier) {Value = eventId});
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
                        $"DELETE {Event.TableName} With(ROWLOCK) WHERE {Event.AggregateId} = @{Event.AggregateId}";
                    command.Parameters.Add(new SqlParameter(Event.AggregateId, SqlDbType.UniqueIdentifier) {Value = aggregateId});
                    command.ExecuteNonQuery();
                });
        }
    }
}
