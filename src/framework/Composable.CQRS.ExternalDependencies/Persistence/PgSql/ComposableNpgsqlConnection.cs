using System.Data;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.SystemCE.ThreadingCE;
using Npgsql;

namespace Composable.Persistence.PgSql
{
    class ComposableNpgsqlConnection : IPoolableConnection, IComposableDbConnection<NpgsqlCommand>
    {
        IDbConnection IComposableDbConnection.Connection => Connection;
        internal NpgsqlConnection Connection { get; }

        public ComposableNpgsqlConnection(string connectionString) => Connection = new NpgsqlConnection(connectionString);

        internal static ComposableNpgsqlConnection Create(string connString) => new ComposableNpgsqlConnection(connString);

        async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
            await syncOrAsync.Run(
                                  () => Connection.Open(),
                                  () => Connection.OpenAsync())
                             .NoMarshalling();

        IDbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
        public NpgsqlCommand CreateCommand() => Connection.CreateCommand();

        public void Dispose() => Connection.Dispose();

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }
}

