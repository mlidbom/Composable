using System.Data.Common;
using System.Threading.Tasks;
using Composable.Persistence.Common;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle
{
    interface IComposableOracleConnection : IPoolableConnection, IComposableDbConnection<OracleCommand>
    {
        internal static IComposableOracleConnection Create(string connString) => new ComposableOracleConnection(connString);

        sealed class ComposableOracleConnection : IComposableOracleConnection
        {
            OracleConnection Connection { get; }

            internal ComposableOracleConnection(string connectionString) => Connection = new OracleConnection(connectionString);

            async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
                await syncOrAsync.Run(
                    () => Connection.Open(),
                    () => Connection.OpenAsync()).NoMarshalling();

            DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();

            public OracleCommand CreateCommand() =>
                Connection.CreateCommand().Mutate(@this => @this.BindByName = true);

            public void Dispose() => Connection.Dispose();

            public ValueTask DisposeAsync() => Connection.DisposeAsync();
        }
    }
}
