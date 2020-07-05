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
            throw new NotImplementedException();
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
(       {C.AggregateId},  {C.InsertedVersion},  {C.EffectiveVersion},  {C.EffectiveOrder},  {C.EventType},  {C.EventId},  {C.UtcTimeStamp},  {C.Event},  {C.InsertAfter}, {C.InsertBefore},  {C.Replaces}) 
VALUES(@{C.AggregateId}, @{C.InsertedVersion}, @{C.EffectiveVersion}, @{C.EffectiveOrder}, @{C.EventType}, @{C.EventId}, @{C.UtcTimeStamp}, @{C.Event}, @{C.InsertAfter},@{C.InsertBefore}, @{C.Replaces})
IF(@{C.EffectiveOrder} IS NULL)
BEGIN
    UPDATE {EventTable.Name} With(READCOMMITTED, ROWLOCK)
    SET {C.EffectiveOrder} = cast({C.InsertionOrder} as {EventTable.ReadOrderType}),
        {C.ReadOrder} = {C.InsertionOrder}
    WHERE {C.EventId} = @{C.EventId}
END
")
                                               //Urgent: implement
                                              //.AddParameter(C.AggregateId, MySqlDbType.UniqueIdentifier, data.AggregateId)
                                              //.AddParameter(C.InsertedVersion, data.RefactoringInformation.InsertedVersion)
                                              //.AddParameter(C.EventType, data.EventType)
                                              //.AddParameter(C.EventId, data.EventId)
                                              //.AddDateTime2Parameter(C.UtcTimeStamp, data.UtcTimeStamp)
                                              //.AddMediumTextParameter(C.Event, data.EventJson)

                                              //.AddNullableParameter(C.EffectiveOrder, MySqlDbType.Decimal, data.RefactoringInformation.EffectiveOrder?.ToSqlDecimal())
                                              //.AddNullableParameter(C.EffectiveVersion, MySqlDbType.Int, data.RefactoringInformation.EffectiveVersion)
                                              //.AddNullableParameter(C.InsertAfter, MySqlDbType.UniqueIdentifier, data.RefactoringInformation.InsertAfter)
                                              //.AddNullableParameter(C.InsertBefore, MySqlDbType.UniqueIdentifier, data.RefactoringInformation.InsertBefore)
                                              //.AddNullableParameter(C.Replaces, MySqlDbType.UniqueIdentifier, data.RefactoringInformation.Replaces)
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
                                                  $@"UPDATE {EventTable.Name} SET {C.EffectiveVersion} = {spec.EffectiveVersion} WHERE {C.EventId} = '{spec.EventId}'").Join(Environment.NewLine);

            _connectionManager.UseConnection(connection => connection.ExecuteNonQuery(commandText));

        }

        public IEventStorePersistenceLayer.EventNeighborhood LoadEventNeighborHood(Guid eventId)
        {
            var lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead = "With(UPDLOCK, READCOMMITTED, ROWLOCK)";

            var selectStatement = $@"
SELECT  {C.EffectiveOrder},        
        (select top 1 {C.EffectiveOrder} from {EventTable.Name} e1 where e1.{C.EffectiveOrder} < {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder} desc) PreviousReadOrder,
        (select top 1 {C.EffectiveOrder} from {EventTable.Name} e1 where e1.{C.EffectiveOrder} > {EventTable.Name}.{C.EffectiveOrder} order by {C.EffectiveOrder}) NextReadOrder
FROM    {EventTable.Name} {lockHintToMinimizeRiskOfDeadlocksByTakingUpdateLockOnInitialRead} 
where {C.EventId} = @{C.EventId}";

            IEventStorePersistenceLayer.EventNeighborhood? neighborhood = null;

            //urgent:implement
            throw new NotImplementedException();
            //_connectionManager.UseCommand(
            //    command =>
            //    {
            //        command.CommandText = selectStatement;

            //        command.Parameters.Add(new SqlParameter(C.EventId, MySqlDbType.UniqueIdentifier) {Value = eventId});
            //        using var reader = command.ExecuteReader();
            //        reader.Read();

            //        var effectiveReadOrder = reader.GetMySqlDecimal(0);
            //        var previousEventReadOrder = reader.GetMySqlDecimal(1);
            //        var nextEventReadOrder = reader.GetMySqlDecimal(2);
            //        neighborhood = new IEventStorePersistenceLayer.EventNeighborhood(effectiveReadOrder: ReadOrder.FromSqlDecimal(effectiveReadOrder),
            //                                                                         previousEventReadOrder: previousEventReadOrder.IsNull ? null : new ReadOrder?(ReadOrder.FromSqlDecimal(previousEventReadOrder)),
            //                                                                         nextEventReadOrder: nextEventReadOrder.IsNull ? null : new ReadOrder?(ReadOrder.FromSqlDecimal(nextEventReadOrder)));
            //    });

            return Assert.Result.NotNull(neighborhood);
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            //urgent:implement
            throw new NotImplementedException();
            //_connectionManager.UseCommand(
            //    command =>
            //    {
            //        command.CommandText +=
            //            $"DELETE {EventTable.Name} With(ROWLOCK) WHERE {C.AggregateId} = @{C.AggregateId}";
            //        command.Parameters.Add(new SqlParameter(C.AggregateId, MySqlDbType.UniqueIdentifier) {Value = aggregateId});
            //        command.ExecuteNonQuery();
            //    });
        }
    }
}
