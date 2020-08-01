using System;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Persistence.Common.AdoCE;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.TransactionsCE;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2
{
    interface IComposableDB2Connection : IPoolableConnection, IComposableDbConnection<DB2Command>
    {
        internal static IComposableDB2Connection Create(string connString) => new ComposableDB2Connection(connString);

        sealed class ComposableDB2Connection : IEnlistmentNotification, IComposableDB2Connection
        {
            DB2Transaction? _db2Transaction;

            DB2Connection Connection { get; }

            internal ComposableDB2Connection(string connectionString) => Connection = new DB2Connection(connectionString);

            async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
                await syncOrAsync.Run(
                    () => Connection.Open(),
                    () => Connection.OpenAsync()).NoMarshalling();

            DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();

            public DB2Command CreateCommand()
            {
                Assert.State.Assert(Connection.IsOpen);
                EnsureParticipatingInAnyTransaction();
                return Connection.CreateCommand().Mutate(@this => @this.Transaction = _db2Transaction);
            }

            public void Dispose()
            {
                Connection.Dispose();
                _db2Transaction?.Dispose();
            }

            public ValueTask DisposeAsync() => Connection.DisposeAsync();

            void IEnlistmentNotification.Commit(Enlistment enlistment)
            {
                _db2Transaction!.Commit();
                DoneWithTransaction(enlistment);
            }

            void IEnlistmentNotification.Rollback(Enlistment enlistment)
            {
                _db2Transaction!.Rollback();
                DoneWithTransaction(enlistment);
            }

            void IEnlistmentNotification.InDoubt(Enlistment enlistment) => DoneWithTransaction(enlistment);

            void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();

            void DoneWithTransaction(Enlistment enlistment)
            {
                _db2Transaction?.Dispose();
                _db2Transaction = null;
                _participatingInLocalIdentifier = null;
                enlistment.Done();
            }

            string? _participatingInLocalIdentifier;
            void EnsureParticipatingInAnyTransaction()
            {
                var ambientTransactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;
                if(ambientTransactionLocalIdentifier != null)
                {
                    if(_participatingInLocalIdentifier == null)
                    {
                        _participatingInLocalIdentifier = ambientTransactionLocalIdentifier;
                        Transaction.Current!.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                        _db2Transaction = Connection.BeginTransaction(Transaction.Current.IsolationLevel.ToDataIsolationLevel());
                    } else if(_participatingInLocalIdentifier != ambientTransactionLocalIdentifier)
                    {
                        throw new Exception($"Somehow switched to a new transaction. Original: {_participatingInLocalIdentifier} new: {ambientTransactionLocalIdentifier}");
                    }
                }
            }
        }
    }
}
