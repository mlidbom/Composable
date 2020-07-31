using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MsSql
{
    class ComposableMsSqlConnection : IPoolableConnection, IComposableDbConnection<SqlCommand>
    {
        IDbConnection IComposableDbConnection.Connection => Connection;
        internal SqlConnection Connection { get; }

        public ComposableMsSqlConnection(string connectionString) => Connection = new SqlConnection(connectionString);

        internal static ComposableMsSqlConnection Create(string connString) => new ComposableMsSqlConnection(connString);

        async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
            await syncOrAsync.Run(
                                  () => Connection.Open(),
                                  () => Connection.OpenAsync())
                             .NoMarshalling();

        IDbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
        public SqlCommand CreateCommand() => Connection.CreateCommand();

        public void Dispose() => Connection.Dispose();

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }
}

