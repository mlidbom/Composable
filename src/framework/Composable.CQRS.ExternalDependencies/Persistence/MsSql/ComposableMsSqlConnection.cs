using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE.ThreadingCE;

namespace Composable.Persistence.MsSql
{
    interface IComposableMsSqlConnection : IPoolableConnection, IComposableDbConnection<SqlCommand>
    {
        internal static IComposableMsSqlConnection Create(string connString) => new ComposableMsSqlConnection(connString);

        sealed class ComposableMsSqlConnection : IComposableMsSqlConnection
        {
            SqlConnection Connection { get; }

            internal ComposableMsSqlConnection(string connectionString) => Connection = new SqlConnection(connectionString);

            async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
                await syncOrAsync.Run(
                    () => Connection.Open(),
                    () => Connection.OpenAsync()).NoMarshalling();

            DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
            public SqlCommand CreateCommand() => Connection.CreateCommand();

            public void Dispose() => Connection.Dispose();

            public ValueTask DisposeAsync() => Connection.DisposeAsync();
        }
    }
}
