using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.SystemCE.TransactionsCE;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2
{
    interface IComposableDB2Connection : IPoolableConnection, IComposableDbConnection<DB2Command>
    {
        //todo: Check if upgrade of IBM.Data.DB2.Core from 1.3.0.100 to 3.1.0.600 means that we should change something.
        internal static IComposableDB2Connection Create(string connString) => new ComposableDB2Connection(connString);

        sealed class ComposableDB2Connection : IComposableDB2Connection
        {
            DB2Transaction? _db2Transaction;

            DB2Connection Connection { get; }

            internal ComposableDB2Connection(string connectionString)
            {
                Connection = new DB2Connection(connectionString);

                _transactionParticipant = new OptimizedLazy<VolatileLambdaTransactionParticipant>(
                    () => new VolatileLambdaTransactionParticipant(
                        onEnlist: () => _db2Transaction = Connection.BeginTransaction(Transaction.Current!.IsolationLevel.ToDataIsolationLevel()),
                        onCommit: () => _db2Transaction!.Commit(),
                        onRollback: () => _db2Transaction!.Rollback(),
                        onTransactionCompleted: _ =>
                        {
                            _db2Transaction?.Dispose();
                            _db2Transaction = null;
                        }));
            }

            async Task IPoolableConnection.OpenAsyncFlex(SyncOrAsync syncOrAsync)
            {
                await syncOrAsync.Run(
                    () => Connection.Open(),
                    () => Connection.OpenAsync()).NoMarshalling();

                _transactionParticipant.Value.EnsureEnlistedInAnyAmbientTransaction();
            }

            DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();

            public DB2Command CreateCommand()
            {
                Assert.State.Assert(Connection.IsOpen);
                _transactionParticipant.Value.EnsureEnlistedInAnyAmbientTransaction();

                return Connection.CreateCommand().Mutate(@this => @this.Transaction = _db2Transaction);
            }

            public void Dispose()
            {
                Connection.Dispose();
                _db2Transaction?.Dispose();
            }

            public ValueTask DisposeAsync() => Connection.DisposeAsync();

            readonly OptimizedLazy<VolatileLambdaTransactionParticipant> _transactionParticipant;
        }
    }
}
