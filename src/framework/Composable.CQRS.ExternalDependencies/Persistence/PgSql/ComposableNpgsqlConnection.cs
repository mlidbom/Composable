using System.Data.Common;
using System.Threading.Tasks;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Npgsql;

namespace Composable.Persistence.PgSql
{
    interface IComposableNpgsqlConnection : IPoolableConnection, IComposableDbConnection<NpgsqlCommand>
    {
        //todo: Check if upgrade of Npgsql from 4.1.4 to 7.0.0 means that we should change something.
        //Npgsql 7.0 Release Notes | Npgsql Documentation https://www.npgsql.org/doc/release-notes/7.0.html
        //Verify Windows-only distributed transactions work on .NET 7.0 · Issue #4581 · npgsql/npgsql · GitHub https://github.com/npgsql/npgsql/issues/4581
        internal static IComposableNpgsqlConnection Create(string connString) => new ComposableNpgsqlConnection(connString);

        sealed class ComposableNpgsqlConnection : IComposableNpgsqlConnection
        {
            NpgsqlConnection Connection { get; }

            public ComposableNpgsqlConnection(string connectionString) => Connection = new NpgsqlConnection(connectionString);

            async Task IPoolableConnection.OpenAsyncFlex(SyncOrAsync syncOrAsync) =>
                await syncOrAsync.Run(
                    () => Connection.Open(),
                    () => Connection.OpenAsync()).NoMarshalling();

            DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
            public NpgsqlCommand CreateCommand() => Connection.CreateCommand();

            public void Dispose() => Connection.Dispose();

            public ValueTask DisposeAsync() => Connection.DisposeAsync();
        }
    }
}
