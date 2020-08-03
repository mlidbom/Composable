using System;
using System.Transactions;

namespace Composable.SystemCE.TransactionsCE
{
    ///<summary>Getting the code for participating in a transaction right is surprisingly tricky and the failures very hard to diagnose.
    /// Use this class for all our transaction participants so we only have to get it right once.</summary>
    abstract class VolatileTransactionParticipant : IEnlistmentNotification
    {
        readonly EnlistmentOptions _enlistmentOptions;
        internal VolatileTransactionParticipant(EnlistmentOptions enlistmentOptions = EnlistmentOptions.None) => _enlistmentOptions = enlistmentOptions;

        protected abstract void OnCommit();
        protected abstract void OnRollback();
        protected abstract void OnPrepare();

        protected virtual void OnEnlist() {}

        protected virtual void OnInDoubt() {}

        Transaction? _enlistedIn;
        internal void EnsureEnlistedInAnyAmbientTransaction()
        {
            var ambientTransaction = Transaction.Current;
            if(ambientTransaction == null) return;

            if(_enlistedIn == null)
            {
                _enlistedIn = ambientTransaction;
                OnEnlist();
                ambientTransaction.EnlistVolatile(this, _enlistmentOptions);
            } else if(_enlistedIn != ambientTransaction)
            {
                throw new Exception($"Somehow switched to a new transaction. Original: {_enlistedIn.TransactionInformation.LocalIdentifier} new: {ambientTransaction.TransactionInformation.LocalIdentifier}");
            }
        }

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            try
            {
                if(_enlistmentOptions.HasFlag(EnlistmentOptions.EnlistDuringPrepareRequired))
                {
                    using var transactionScope = new TransactionScope(_enlistedIn!);
                    OnPrepare();
                    transactionScope.Complete();
                } else
                {
                    OnPrepare();
                }

                preparingEnlistment.Prepared();
            }
            catch(Exception exception)
            {
                preparingEnlistment.ForceRollback(exception);
            }
        }

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            OnCommit();
            enlistment.Done();
            _enlistedIn = null;
        }

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            OnRollback();
            enlistment.Done();
            _enlistedIn = null;
        }

        void IEnlistmentNotification.InDoubt(Enlistment enlistment)
        {
            OnInDoubt();
            enlistment.Done();
            _enlistedIn = null;
        }
    }
}
