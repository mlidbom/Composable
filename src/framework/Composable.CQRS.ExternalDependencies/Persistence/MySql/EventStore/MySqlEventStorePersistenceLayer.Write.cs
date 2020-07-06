using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.SqlServer.EventStore;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.System;
using MySql.Data.MySqlClient;
using C = Composable.Persistence.Common.EventStore.EventTable.Columns;
using ReadOrder = Composable.Persistence.EventStore.IEventStorePersistenceLayer.ReadOrder;

namespace Composable.Persistence.MySql.EventStore
{
    partial class MySqlEventStorePersistenceLayer
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
                                                   //urgent: explore mysql alternatives to commented out hints .
                                                   $@"
INSERT {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
(       {C.AggregateId},  {C.InsertedVersion},  {C.EffectiveVersion},  {C.EffectiveOrder},  {C.EventType},  {C.EventId},  {C.UtcTimeStamp},  {C.Event},  {C.InsertAfter}, {C.InsertBefore},  {C.Replaces}) 
VALUES(@{C.AggregateId}, @{C.InsertedVersion}, @{C.EffectiveVersion}, @{C.EffectiveOrder}, @{C.EventType}, @{C.EventId}, @{C.UtcTimeStamp}, @{C.Event}, @{C.InsertAfter},@{C.InsertBefore}, @{C.Replaces});


IF @{C.EffectiveOrder} IS NULL THEN
    UPDATE {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
    SET {C.EffectiveOrder} = cast({C.InsertionOrder} as {EventTable.ReadOrderType}),
        {C.ReadOrder} = {C.InsertionOrder}
    WHERE {C.EventId} = @{C.EventId};
END IF;
")
                                              .AddParameter(C.AggregateId, data.AggregateId)
                                              .AddParameter(C.InsertedVersion, data.RefactoringInformation.InsertedVersion)
                                              .AddParameter(C.EventType, data.EventType)
                                              .AddParameter(C.EventId, data.EventId)
                                              .AddDateTime2Parameter(C.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddMediumTextParameter(C.Event, data.EventJson)

                                              .AddNullableParameter(C.EffectiveOrder, MySqlDbType.VarChar, data.RefactoringInformation.EffectiveOrder?.ToString())
                                              .AddNullableParameter(C.EffectiveVersion, MySqlDbType.Int32, data.RefactoringInformation.EffectiveVersion)
                                              .AddNullableParameter(C.InsertAfter, MySqlDbType.Guid, data.RefactoringInformation.InsertAfter)
                                              .AddNullableParameter(C.InsertBefore, MySqlDbType.Guid, data.RefactoringInformation.InsertBefore)
                                              .AddNullableParameter(C.Replaces, MySqlDbType.Guid, data.RefactoringInformation.Replaces)
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
                                                  $@"UPDATE {EventTable.Name} SET {C.EffectiveVersion} = {spec.EffectiveVersion} WHERE {C.EventId} = '{spec.EventId}';").Join(Environment.NewLine);

            _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

        }

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            //urgent: Find MySql equivalent
            //var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

            var selectStatement = $@"
SELECT  {C.EffectiveOrder},        
        (select cast({C.EffectiveOrder} as char(39)) from {EventTable.Name} e1 where e1.{C.EffectiveOrder} < {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} desc limit 1) PreviousReadOrder,
        (select cast({C.EffectiveOrder} as char(39)) from {EventTable.Name} e1 where e1.{C.EffectiveOrder} > {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} limit 1) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {C.EventId} = @{C.EventId}";

            IEventStorePersistenceLayer.EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;

                    command.Parameters.Add(new MySqlParameter(C.EventId, MySqlDbType.Guid) { Value = eventId });
                    using var reader = command.ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = reader.GetString(0).Replace(",", ".");
                    var previousEventReadOrder = (reader[1] as string)?.Replace(",", ".");
                    var nextEventReadOrder = (reader[2] as string)?.Replace(",", ".");
                    neighborhood = new IEventStorePersistenceLayer.EventNeighborhood(effectiveReadOrder: ReadOrder.Parse(effectiveReadOrder),
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
                    //urgent: Find equivalent to rowlock hint.
                    command.CommandText +=
                        $"DELETE FROM {EventTable.Name} /*With(ROWLOCK)*/ WHERE {C.AggregateId} = @{C.AggregateId};";
                    command.Parameters.Add(new MySqlParameter(C.AggregateId, MySqlDbType.Guid) { Value = aggregateId });
                    command.ExecuteNonQuery();
                });
        }
    }
}
