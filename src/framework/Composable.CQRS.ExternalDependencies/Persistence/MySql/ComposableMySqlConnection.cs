using System.Data.Common;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql
{
    interface IComposableMySqlConnection : IPoolableConnection, IComposableDbConnection<MySqlCommand>
    {
        internal static IComposableMySqlConnection Create(string connString) => new ComposableMySqlConnection(connString);

        sealed class ComposableMySqlConnection : IComposableMySqlConnection
        {
            MySqlConnection Connection { get; }

            internal ComposableMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

            async Task IPoolableConnection.OpenAsyncFlex(SyncOrAsync syncOrAsync) =>
                await syncOrAsync.Run(
                    () => Connection.Open(),
                    () => Connection.OpenAsync()).NoMarshalling();

            DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
            public MySqlCommand CreateCommand() => Connection.CreateCommand();

            public void Dispose() => Connection.Dispose();

            public ValueTask DisposeAsync() => Connection.DisposeAsync();
        }
    }
}
