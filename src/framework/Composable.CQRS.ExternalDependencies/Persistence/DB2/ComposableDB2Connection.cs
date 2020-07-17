using System;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.System.Linq;
using Composable.SystemExtensions.TransactionsCE;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2
{
    class ComposableDB2Connection : IDisposable, IAsyncDisposable, IEnlistmentNotification
    {
        readonly DB2Connection _connection;
        DB2Transaction? _db2Transaction;
        public ComposableDB2Connection(string connectionString) => _connection = new DB2Connection(connectionString);

        internal DB2Connection Connection => _connection;

        public void Open()
        {
            _connection.Open();
            EnsureParticipatingInTransaction();
        }

        public DB2Command CreateCommand()
        {
            Assert.State.Assert(_connection.IsOpen);
            EnsureParticipatingInTransaction();
            return _connection.CreateCommand().Mutate(@this => @this.Transaction = _db2Transaction);
        }

        public void Dispose()
        {
            _connection.Dispose();
            _db2Transaction?.Dispose();
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();


        public void Commit(Enlistment enlistment)
        {
            _db2Transaction!.Commit();
            DoneWithTransaction(enlistment);
        }

        public void Rollback(Enlistment enlistment)
        {
            _db2Transaction!.Rollback();
            DoneWithTransaction(enlistment);
        }

        public void InDoubt(Enlistment enlistment) => DoneWithTransaction(enlistment);

        public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();

        void DoneWithTransaction(Enlistment enlistment)
        {
            _db2Transaction?.Dispose();
            _db2Transaction = null;
            _participatingIn = null;
            enlistment.Done();
        }


        Transaction? _participatingIn;
        void EnsureParticipatingInTransaction()
        {
            var ambientTransaction = Transaction.Current;
            if(ambientTransaction != null)
            {
                if(_participatingIn == null)
                {
                    _participatingIn = ambientTransaction;
                    ambientTransaction.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                    _db2Transaction = _connection.BeginTransaction(Transaction.Current.IsolationLevel.ToDataIsolationLevel());
                }
                else if(_participatingIn != ambientTransaction)
                {
                    throw new Exception($"Somehow switched to a new transaction. Original: {_participatingIn.TransactionInformation.LocalIdentifier} new: {ambientTransaction.TransactionInformation.LocalIdentifier}");
                }
            }
        }
    }
}

