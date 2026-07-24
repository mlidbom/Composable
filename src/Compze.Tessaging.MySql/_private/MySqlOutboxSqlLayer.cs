using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.MySql._internal;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using Compze.TypeIdentifiers.Interning;
using CounterTable = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxDeliveryStreamCountersSchemaStrings;
using DispatchingTable = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessageDispatchingTableSchemaStrings;
using TessageTable = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.OutboxTessagesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql._private;

partial class MySqlOutboxSqlLayer(IMySqlConnectionPool connectionFactory, MySqlSqlLayerSchemaManager schemaManager, ITypeIdInterner typeIdInterner, EndpointTableSet tables) : ITessagingSqlLayer.IOutboxSqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly ITypeIdInterner _typeIdInterner = typeIdInterner;
   readonly EndpointTableSet _tables = tables;

   public async Task<IReadOnlyDictionary<EndpointId, long>> SaveTessageAsync(ITessagingSqlLayer.OutboxTessageWithReceivers tessageWithReceivers)
   {
      // Intern before opening a connection: interning may hit the database, and nesting a second connection
      // inside a held one deadlocks the pool.
      var internedTypeId = _typeIdInterner.GetOrInternId(tessageWithReceivers.TypeId);
      var assignedSequenceNumbers = await AssignDeliveryStreamSequenceNumbersAsync(tessageWithReceivers.ReceiverEndpointIds).caf();
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT {_tables.OutboxTessages}
                               ({TessageTable.TessageId},  {TessageTable.TypeId}, {TessageTable.SerializedTessage})
                       VALUES (@{TessageTable.TessageId}, @{TessageTable.TypeId}, @{TessageTable.SerializedTessage});

                   """)
              .AddParameter(TessageTable.TessageId, tessageWithReceivers.TessageId.Value)
              .AddParameter(TessageTable.TypeId, internedTypeId)
               //performance: Like with the tevent store, keep all framework properties out of the JSON and put it into separate columns instead. For tevents. Reuse a pre-serialized instance from the persisting to the tevent store.
              .AddMediumTextParameter(TessageTable.SerializedTessage, tessageWithReceivers.SerializedTessage)
              .AddParameter(DispatchingTable.IsReceived, 0);

            tessageWithReceivers.ReceiverEndpointIds.ForEach(
               (endpointId, index)
                  => command.AppendCommandText($"""

                                                INSERT {_tables.OutboxTessageDispatching}
                                                            ({DispatchingTable.TessageId},  {DispatchingTable.EndpointId},          {DispatchingTable.DeliveryStreamSequenceNumber},          {DispatchingTable.IsReceived})
                                                    VALUES (@{DispatchingTable.TessageId}, @{DispatchingTable.EndpointId}_{index}, @{DispatchingTable.DeliveryStreamSequenceNumber}_{index}, @{DispatchingTable.IsReceived});

                                                """)
                            .AddParameter($"{DispatchingTable.EndpointId}_{index}", endpointId.Value)
                            .AddParameter($"{DispatchingTable.DeliveryStreamSequenceNumber}_{index}", assignedSequenceNumbers[endpointId]));

            return await command.ExecuteNonQueryAsync().caf();
         }).caf();
      return assignedSequenceNumbers;
   }

   //The counter row's lock, taken by this upsert inside the caller's save transaction and held to its commit, serializes the
   //pair's commits: sequence order is commit order. One receiver at a time, in OutboxTessageWithReceivers' deterministic
   //order - the lock order that keeps concurrent saves to overlapping receiver sets deadlock-free.
   async Task<IReadOnlyDictionary<EndpointId, long>> AssignDeliveryStreamSequenceNumbersAsync(IReadOnlyList<EndpointId> receiverEndpointIds)
   {
      var assigned = new Dictionary<EndpointId, long>();
      foreach(var endpointId in receiverEndpointIds)
      {
         await _connectionFactory.UseCommandAsync(
            async command => await command
                      .SetCommandText(
                          $"""

                           INSERT INTO {_tables.OutboxDeliveryStreamCounters}
                                       ({CounterTable.EndpointId}, {CounterTable.LastAssignedSequenceNumber})
                               VALUES (@{CounterTable.EndpointId}, 1)
                           ON DUPLICATE KEY UPDATE {CounterTable.LastAssignedSequenceNumber} = {CounterTable.LastAssignedSequenceNumber} + 1;

                           """)
                      .AddParameter(CounterTable.EndpointId, endpointId.Value)
                      .ExecuteNonQueryAsync().caf()).caf();

         //Read back on the same transaction-affine connection: the row is ours until commit - the upsert's lock guarantees
         //the value read is the one just assigned.
         assigned[endpointId] = await _connectionFactory.UseCommandAsync(
            async command => (long)(await command
                      .SetCommandText(
                          $"""

                           SELECT {CounterTable.LastAssignedSequenceNumber}
                           FROM {_tables.OutboxDeliveryStreamCounters}
                           WHERE {CounterTable.EndpointId} = @{CounterTable.EndpointId};

                           """)
                      .AddParameter(CounterTable.EndpointId, endpointId.Value)
                      .ExecuteScalarAsync().caf())!).caf();
      }
      return assigned;
   }

   public async Task<long> GetDeliveryStreamPredecessorSequenceNumberAsync(EndpointId receiverId, long sequenceNumber) =>
      await _connectionFactory.UseCommandAsync(
         async command => (long)(await command
                   .SetCommandText(
                       $"""

                        SELECT COALESCE(MAX({DispatchingTable.DeliveryStreamSequenceNumber}), 0)
                        FROM {_tables.OutboxTessageDispatching}
                        WHERE {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                          AND {DispatchingTable.DeliveryStreamSequenceNumber} < @{DispatchingTable.DeliveryStreamSequenceNumber}
                          AND NOT ({DispatchingTable.IsStranded} = 1 AND {DispatchingTable.IsReceived} = 0) -- An unreceived stranded row awaits explicit resolution and will never reach the receiver.

                        """)
                   .AddParameter(DispatchingTable.EndpointId, receiverId.Value)
                   .AddParameter(DispatchingTable.DeliveryStreamSequenceNumber, sequenceNumber)
                   .ExecuteScalarAsync().caf())!).caf();

   public async Task<ITessagingSqlLayer.MarkAsReceivedResult> MarkAsReceivedAsync(TessageId tessageId, EndpointId endpointId)
   {
      var affectedRows = await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.OutboxTessageDispatching}
                            SET {DispatchingTable.IsReceived} = 1
                        WHERE {DispatchingTable.TessageId} = @{DispatchingTable.TessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                            AND {DispatchingTable.IsReceived} = 0

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId.Value)
                   .AddParameter(DispatchingTable.EndpointId, endpointId.Value)
                   .ExecuteNonQueryAsync().caf()).caf();

      return affectedRows == 1
                ? ITessagingSqlLayer.MarkAsReceivedResult.Initial
                : ITessagingSqlLayer.MarkAsReceivedResult.WasAlreadyMarked;
   }

   public async Task RecordDeliveryFailureAsync(TessageId tessageId, EndpointId endpointId, string failureReason)
   {
      await _connectionFactory.UseCommandAsync(
         async command => await command
                   .SetCommandText(
                       $"""

                        UPDATE {_tables.OutboxTessageDispatching} 
                            SET {DispatchingTable.RetryCount} = {DispatchingTable.RetryCount} + 1,
                                {DispatchingTable.LastAttemptTime} = @{DispatchingTable.LastAttemptTime},
                                {DispatchingTable.FailureReason} = @{DispatchingTable.FailureReason}
                        WHERE {DispatchingTable.TessageId} = @{DispatchingTable.TessageId}
                            AND {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}

                        """)
                   .AddParameter(DispatchingTable.TessageId, tessageId.Value)
                   .AddParameter(DispatchingTable.EndpointId, endpointId.Value)
                   .AddDateTime2Parameter(DispatchingTable.LastAttemptTime, DateTime.UtcNow)
                   .AddMediumTextParameter(DispatchingTable.FailureReason, failureReason)
                   .ExecuteNonQueryAsync().caf()).caf();
   }

   public async Task<IReadOnlyList<ITessagingSqlLayer.UndeliveredTessage>> GetUndeliveredTessagesForEndpointAsync(EndpointId endpointId)
   {
      // The TypeId column holds an interned int. Resolve it to the canonical type string AFTER the reader has
      // closed — resolving during the read could open a second connection on a cache miss while the reader is held.
      var raw = await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var rows = new List<(TessageId TessageId, long SequenceNumber, int TypeId, string Body)>();

            command
               .SetCommandText(
                   $"""

                    SELECT m.{TessageTable.TessageId},
                           d.{DispatchingTable.DeliveryStreamSequenceNumber},
                           m.{TessageTable.TypeId},
                           m.{TessageTable.SerializedTessage}
                    FROM {_tables.OutboxTessages} m
                    INNER JOIN {_tables.OutboxTessageDispatching} d ON m.{TessageTable.TessageId} = d.{DispatchingTable.TessageId}
                    WHERE d.{DispatchingTable.IsReceived} = 0
                      AND d.{DispatchingTable.IsStranded} = 0 -- A stranded tessage awaits explicit resolution (dev_docs/WIP/peer-administration.md), never delivery.
                      AND d.{DispatchingTable.EndpointId} = @endpointId
                    ORDER BY d.{DispatchingTable.DeliveryStreamSequenceNumber} -- The pair's stream order, which is commit order: recovery re-establishes in-order delivery.

                    """)
               .AddParameter("endpointId", endpointId.Value);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               rows.Add((new TessageId(reader.GetGuid(0)),
                         reader.GetInt64(1),
                         reader.GetInt32(2),
                         reader.GetString(3)));
            }

            return rows;
         }).caf();

      return [..raw.Select(row => new ITessagingSqlLayer.UndeliveredTessage(
                              tessageId: row.TessageId,
                              deliveryStreamSequenceNumber: row.SequenceNumber,
                              typeId: _typeIdInterner.GetTypeId(row.TypeId),
                              serializedTessage: row.Body))];
   }

   public async Task StrandUndeliveredTessagesAsync(EndpointId endpointId, IReadOnlyList<TessageId> tessageIds)
   {
      if(tessageIds.Count == 0) return;
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   UPDATE {_tables.OutboxTessageDispatching}
                       SET {DispatchingTable.IsStranded} = 1
                   WHERE {DispatchingTable.EndpointId} = @{DispatchingTable.EndpointId}
                     AND {DispatchingTable.TessageId} IN ( {TessageIdParameterList(tessageIds.Count)} )
                     AND {DispatchingTable.IsReceived} = 0 -- Received between the reconciliation read and this statement: delivered before the shrink took effect, nothing to strand.

                   """)
              .AddParameter(DispatchingTable.EndpointId, endpointId.Value);
            tessageIds.ForEach((tessageId, index) => command.AddParameter($"{DispatchingTable.TessageId}_{index}", tessageId.Value));
            return await command.ExecuteNonQueryAsync().caf();
         }).caf();
   }

   static string TessageIdParameterList(int tessageIdCount) =>
      string.Join(", ", Enumerable.Range(0, tessageIdCount).Select(index => $"@{DispatchingTable.TessageId}_{index}"));

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
