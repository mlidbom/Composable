using Compze.Tessaging.Endpoints;
using Compze.Sql.Common._internal;
using Compze.Sql.MySql._internal;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.SqlLayer;
using PeersSchema = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.PeersDatabaseSchemaStrings;
using Types = Compze.Tessaging._internal.SqlLayer.ITessagingSqlLayer.PeerHandledTessageTypesDatabaseSchemaStrings;

namespace Compze.Tessaging.MySql._private;

partial class MySqlPeerRegistrySqlLayer(IMySqlConnectionPool connectionFactory, MySqlSqlLayerSchemaManager schemaManager, EndpointTableSet tables) : ITessagingSqlLayer.IPeerRegistrySqlLayer
{
   readonly IMySqlConnectionPool _connectionFactory = connectionFactory;
   readonly MySqlSqlLayerSchemaManager _schemaManager = schemaManager;
   readonly EndpointTableSet _tables = tables;

   public async Task SaveAdvertisementAsync(EndpointId peerId, IReadOnlySet<string> handledTessageTypes)
   {
      await _connectionFactory.UseCommandAsync(
         async command =>
         {
            command
              .SetCommandText(
                  $"""

                   INSERT IGNORE INTO {_tables.Peers} ({PeersSchema.EndpointId}) VALUES (@{PeersSchema.EndpointId});

                   DELETE FROM {_tables.PeerHandledTessageTypes} WHERE {Types.EndpointId} = @{Types.EndpointId};

                   """)
              .AddParameter(PeersSchema.EndpointId, peerId.Value);

            handledTessageTypes.ForEach(
               (handledTessageType, index)
                  => command.AppendCommandText($"""

                                                INSERT INTO {_tables.PeerHandledTessageTypes}
                                                            ({Types.EndpointId},  {Types.HandledTessageType})
                                                    VALUES (@{Types.EndpointId}, @{Types.HandledTessageType}_{index});

                                                """).AddMediumTextParameter($"{Types.HandledTessageType}_{index}", handledTessageType));

            return await command.ExecuteNonQueryAsync().caf();
         }).caf();
   }

   public async Task<IReadOnlyList<ITessagingSqlLayer.PersistedPeer>> GetPeersAsync()
   {
      var rows = await _connectionFactory.UseCommandAsync(
         async command =>
         {
            var raw = new List<(Guid EndpointId, string? HandledTessageType)>();

            command.SetCommandText(
               $"""

                SELECT p.{PeersSchema.EndpointId}, t.{Types.HandledTessageType}
                FROM {_tables.Peers} p
                LEFT JOIN {_tables.PeerHandledTessageTypes} t ON p.{PeersSchema.EndpointId} = t.{Types.EndpointId}

                """);

            var reader = await command.ExecuteReaderAsync().caf();
            await using var _ = reader.caf();
            while(await reader.ReadAsync().caf())
            {
               raw.Add((reader.GetGuid(0), await reader.IsDBNullAsync(1).caf() ? null : reader.GetString(1)));
            }

            return raw;
         }).caf();

      return [..rows.GroupBy(row => row.EndpointId)
                    .Select(peer => new ITessagingSqlLayer.PersistedPeer(
                               new EndpointId(peer.Key),
                               peer.Where(row => row.HandledTessageType != null).Select(row => row.HandledTessageType!).ToHashSet()))];
   }

   public async Task InitAsync() => await _schemaManager.EnsureSchemaInitializedAsync().caf();
}
