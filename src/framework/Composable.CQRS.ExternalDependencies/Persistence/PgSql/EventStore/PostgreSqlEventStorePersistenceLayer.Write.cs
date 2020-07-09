using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.System;
using Npgsql;
using NpgsqlTypes;
using C = Composable.Persistence.Common.EventStore.EventTable.Columns;
using ReadOrder = Composable.Persistence.EventStore.PersistenceLayer.ReadOrder;

namespace Composable.Persistence.PgSql.EventStore
{
    partial class PgSqlEventStorePersistenceLayer
    {
        const int PrimaryKeyViolationSqlErrorNumber = 23505;

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
                                                   //urgent: explore PgSql alternatives to commented out hints .
                                                   $@"
{(data.AggregateVersion > 0 ? "" :$@"
insert into AggregateLock(AggregateId) values('{data.AggregateId}';)
                        ")}
INSERT INTO {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
(       {C.AggregateId},  {C.InsertedVersion},  {C.EffectiveVersion},  {C.EffectiveOrder},                                      {C.EventType},  {C.EventId},  {C.UtcTimeStamp},  {C.Event},  {C.TargetEvent}, {C.RefactoringType}) 
VALUES(@{C.AggregateId}, @{C.InsertedVersion}, @{C.EffectiveVersion}, cast(@{C.EffectiveOrder} as {EventTable.ReadOrderType}), @{C.EventType}, @{C.EventId}, @{C.UtcTimeStamp}, @{C.Event}, @{C.TargetEvent},@{C.RefactoringType});

{(data.StorageInformation.ReadOrder != null ? "":$@"
UPDATE {EventTable.Name} /*With(READCOMMITTED, ROWLOCK)*/
        SET {C.EffectiveOrder} = cast({C.InsertionOrder} as {EventTable.ReadOrderType}),
            {C.ReadOrder} = {C.InsertionOrder}
        WHERE {C.EventId} = @{C.EventId};
")}

")
                                              .AddParameter(C.AggregateId, data.AggregateId)
                                              .AddParameter(C.InsertedVersion, data.StorageInformation.InsertedVersion)
                                              .AddParameter(C.EventType, data.EventType)
                                              .AddParameter(C.EventId, data.EventId)
                                              .AddDateTime2Parameter(C.UtcTimeStamp, data.UtcTimeStamp)
                                              .AddMediumTextParameter(C.Event, data.EventJson)

                                              .AddNullableParameter(C.EffectiveOrder, NpgsqlDbType.Varchar, data.StorageInformation.ReadOrder?.ToString())
                                              .AddNullableParameter(C.EffectiveVersion, NpgsqlDbType.Integer, data.StorageInformation.EffectiveVersion)
                                              .AddNullableParameter(C.TargetEvent, NpgsqlDbType.Varchar, data.StorageInformation.RefactoringInformation?.TargetEvent.ToString())
                                              .AddNullableParameter(C.RefactoringType, NpgsqlDbType.Smallint, data.StorageInformation.RefactoringInformation?.RefactoringType == null ? null : (byte?)data.StorageInformation.RefactoringInformation.RefactoringType)
                                              .ExecuteNonQuery());
                    }
                    catch(PostgresException e) when(e.SqlState == PrimaryKeyViolationSqlErrorNumber.ToString())
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
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "";

            var selectStatement = $@"
SELECT  cast({C.EffectiveOrder} as varchar) as CharEffectiveOrder,        
        (select cast({C.EffectiveOrder} as varchar) as CharEffectiveOrder from {EventTable.Name} e1 where e1.{C.EffectiveOrder} < {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} desc limit 1) PreviousReadOrder,
        (select cast({C.EffectiveOrder} as varchar) as CharEffectiveOrder from {EventTable.Name} e1 where e1.{C.EffectiveOrder} > {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} limit 1) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {C.EventId} = @{C.EventId}
{CreateLockHint(takeWriteLock:true)}";

            EventNeighborhood? neighborhood = null;

            _connectionManager.UseCommand(
                command =>
                {
                    command.CommandText = selectStatement;

                    command.AddParameter(C.EventId, eventId);
                    using var reader = command.ExecuteReader();
                    reader.Read();

                    var effectiveReadOrder = reader.GetString(0).Replace(",", ".");
                    var previousEventReadOrder = (reader[1] as string)?.Replace(",", ".");
                    var nextEventReadOrder = (reader[2] as string)?.Replace(",", ".");
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
                    //urgent: Find equivalent to rowlock hint.
                    command.CommandText +=
                        $"DELETE FROM {EventTable.Name} /*With(ROWLOCK)*/ WHERE {C.AggregateId} = @{C.AggregateId};";
                    command.AddParameter(C.AggregateId, aggregateId);
                    command.ExecuteNonQuery();
                });
        }
    }
}
