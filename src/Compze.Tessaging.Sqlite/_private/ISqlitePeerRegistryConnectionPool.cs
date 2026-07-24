using Compze.Sql.Sqlite._internal;

namespace Compze.Tessaging.Sqlite._private;

///<summary>The connection pool for the peer registry's <em>own</em> database — an auxiliary SQLite database, separate from the<br/>
/// one that holds the endpoint's domain data.</summary>
///<remarks>SQLite permits one writer per database, so Compze serializes whole write transactions per database behind an<br/>
/// in-process write gate (see <c>CompzeSqliteConnection</c>) — held by a handling transaction from its first write (the inbox<br/>
/// claim) until it completes. Recording a peer's advertisement is fact-keeping — the fetch happened — and must complete while<br/>
/// such a transaction is open: a handler's send can be waiting, inside its open transaction, for exactly the peer knowledge the<br/>
/// recording provides. Were the peer tables in the domain database, the recording's commit would queue behind the very<br/>
/// transaction awaiting it, stalling every such first contact for the full handler-availability patience. In its own database,<br/>
/// the recording commits behind its own write gate, untouched by domain transactions.</remarks>
///<remarks>This is its own type for one reason: the container resolves components by service type and cannot hold two<br/>
/// registrations of the same type, so the peer registry's pool needs a type distinct from the domain<br/>
/// <see cref="ISqliteConnectionPool"/> to coexist with it. It adds no members — it is the domain pool's behaviour pointed at a<br/>
/// different database.</remarks>
interface ISqlitePeerRegistryConnectionPool : ISqliteConnectionPool
{
   ///<summary>The domain <see cref="ISqliteConnectionPool.SqliteConnectionPool"/> behaviour, tagged as the peer registry's pool<br/>
   /// so the container can hold both. Construct it for the peer registry database reached through<br/>
   /// <paramref name="getConnectionString"/>.</summary>
   public sealed class Pool(Func<string> getConnectionString) : SqliteConnectionPool(getConnectionString), ISqlitePeerRegistryConnectionPool;
}
