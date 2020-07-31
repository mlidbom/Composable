using System.Data;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE.ThreadingCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql
{
    class ComposableMySqlConnection : IPoolableConnection, IComposableDbConnection<MySqlCommand>
    {
        IDbConnection IComposableDbConnection.Connection => Connection;
        internal MySqlConnection Connection { get; }

        public ComposableMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

        internal static ComposableMySqlConnection Create(string connString) => new ComposableMySqlConnection(connString);

        async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
            await syncOrAsync.Run(
                                  () => Connection.Open(),
                                  () => Connection.OpenAsync())
                             .NoMarshalling();

        IDbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
        public MySqlCommand CreateCommand() => Connection.CreateCommand();

        public void Dispose() => Connection.Dispose();

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }
}

