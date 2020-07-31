using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE.ThreadingCE;
using Npgsql;

namespace Composable.Persistence.PgSql
{
    interface IComposableNpgsqlConnection : IPoolableConnection, IComposableDbConnection<NpgsqlCommand>
    {
        internal static IComposableNpgsqlConnection Create(string connString) => new ComposableNpgsqlConnection(connString);

        class ComposableNpgsqlConnection : IComposableNpgsqlConnection
        {
            IDbConnection IComposableDbConnection.Connection => Connection;
            internal NpgsqlConnection Connection { get; }

            public ComposableNpgsqlConnection(string connectionString) => Connection = new NpgsqlConnection(connectionString);

            async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
                await syncOrAsync.Run(
                                      () => Connection.Open(),
                                      () => Connection.OpenAsync())
                                 .NoMarshalling();

            DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
            public NpgsqlCommand CreateCommand() => Connection.CreateCommand();

            public void Dispose() => Connection.Dispose();

            public ValueTask DisposeAsync() => Connection.DisposeAsync();
        }
    }
}

