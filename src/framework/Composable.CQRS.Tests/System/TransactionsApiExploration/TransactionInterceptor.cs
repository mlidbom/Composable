using System;
using System.Transactions;

namespace Composable.Tests.System.TransactionsApiExploration
{
    class TransactionInterceptor : IEnlistmentNotification
    {
        readonly Transaction _transaction;
        readonly Action<PreparingEnlistment> _onPrepare;
        readonly Action<Enlistment> _onCommit;
        readonly Action<Enlistment> _onRollback;
        readonly Action<Enlistment> _onInDoubt;

        public TransactionInterceptor(Transaction transaction,
                                      Action<PreparingEnlistment> onPrepare = null,
                                      Action<Enlistment> onCommit = null,
                                      Action<Enlistment> onRollback = null,
                                      Action<Enlistment> onInDoubt = null)
        {
            _transaction = transaction;
            _onPrepare = onPrepare;
            _onCommit = onCommit;
            _onRollback = onRollback;
            _onInDoubt = onInDoubt;
            transaction.EnlistVolatile(this, EnlistmentOptions.None);
            transaction.TransactionCompleted += (sender, args) => CompletedInformation = args.Transaction.TransactionInformation;
        }

        public TransactionInformation CompletedInformation { get; private set; }

        public TransactionStatus Status => CompletedInformation?.Status ?? _transaction.TransactionInformation.Status;

        public bool CommittedSuccessfully => Status == TransactionStatus.Committed && PrepareCalled && CommitCalled && !RollbackCalled && !InDoubtCalled;

        public bool PrepareCalled { get; private set; }
        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            PrepareCalled = true;

            if(_onPrepare != null)
            {
                _onPrepare(preparingEnlistment);
            } else
            {
                preparingEnlistment.Prepared();
            }
        }

        public bool CommitCalled { get; private set; }
        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            CommitCalled = true;

            if(_onCommit != null)
            {
                _onCommit.Invoke(enlistment);
            } else
            {
                enlistment.Done();
            }
        }

        public bool RollbackCalled { get; private set; }
        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            RollbackCalled = true;

            if(_onRollback != null)
            {
                _onRollback.Invoke(enlistment);
            } else
            {
                enlistment.Done();
            }
        }

        public bool InDoubtCalled { get; private set; }
        void IEnlistmentNotification.InDoubt(Enlistment enlistment)
        {
            InDoubtCalled = true;

            if(_onInDoubt != null)
            {
                _onInDoubt.Invoke(enlistment);
            } else
            {
                enlistment.Done();
            }
        }
    }
}
