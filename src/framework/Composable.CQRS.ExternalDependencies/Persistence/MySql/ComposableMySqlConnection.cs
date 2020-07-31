using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE.ThreadingCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql
{
    interface IComposableMySqlConnection : IPoolableConnection, IComposableDbConnection<MySqlCommand> {}

    class ComposableMySqlConnection : IComposableMySqlConnection
    {
        IDbConnection IComposableDbConnection.Connection => Connection;
        internal MySqlConnection Connection { get; }

        ComposableMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

        internal static IComposableMySqlConnection Create(string connString) => new ComposableMySqlConnection(connString);

        async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
            await syncOrAsync.Run(
                                  () => Connection.Open(),
                                  () => Connection.OpenAsync())
                             .NoMarshalling();

        DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
        public MySqlCommand CreateCommand() => Connection.CreateCommand();

        public void Dispose() => Connection.Dispose();

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }
}

