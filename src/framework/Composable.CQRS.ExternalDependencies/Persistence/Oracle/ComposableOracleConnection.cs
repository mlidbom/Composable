using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE.ThreadingCE;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle
{
    class ComposableOracleConnection : IPoolableConnection, IComposableDbConnection<OracleCommand>
    {
        IDbConnection IComposableDbConnection.Connection => Connection;
        internal OracleConnection Connection { get; }

        public ComposableOracleConnection(string connectionString) => Connection = new OracleConnection(connectionString);

        internal static ComposableOracleConnection Create(string connString) => new ComposableOracleConnection(connString);

        async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
            await syncOrAsync.Run(() => Connection.Open(),
                                  () => Connection.OpenAsync()).NoMarshalling();

        DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
        public OracleCommand CreateCommand() => Connection.CreateCommand();

        public void Dispose() => Connection.Dispose();

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }
}
