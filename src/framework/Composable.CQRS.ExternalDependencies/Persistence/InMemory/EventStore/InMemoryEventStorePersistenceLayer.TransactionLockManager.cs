using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.Collections;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Persistence.InMemory.EventStore
{
    partial class InMemoryEventStorePersistenceLayer
    {
        class TransactionLockManager
        {
            readonly Dictionary<Guid, TransactionWideLock> _aggregateGuards = new Dictionary<Guid, TransactionWideLock>();

            public TResult WithTransactionWideLock<TResult>(Guid aggregateId, Func<TResult> func) => WithTransactionWideLock(aggregateId, true, func);
            public TResult WithTransactionWideLock<TResult>(Guid aggregateId, bool takeWriteLock, Func<TResult> func)
            {
                if(Transaction.Current != null)
                {
                    var @lock = _aggregateGuards.GetOrAdd(aggregateId, () => new TransactionWideLock());
                    @lock.AwaitAccess(takeWriteLock);
                }

                return func();
            }

            public void WithTransactionWideLock(Guid aggregateId, Action action) => WithTransactionWideLock(aggregateId, true, () =>
            {
                action();
                return 1;
            });

            class TransactionWideLock
            {
                public TransactionWideLock() => Guard = ResourceGuard.WithTimeout(1.Minutes());

                public void AwaitAccess(bool takeWriteLock)
                {
                    if(OwningTransactionLocalId.Length > 0 && !takeWriteLock)
                    {
                        return;
                    }

                    var currentTransactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
                    if(currentTransactionId != OwningTransactionLocalId)
                    {
                        var @lock = Guard.AwaitUpdateLock();
                        Transaction.Current.OnCompleted(() =>
                        {
                            OwningTransactionLocalId = string.Empty;
                            @lock.Dispose();
                        });
                        OwningTransactionLocalId = currentTransactionId;
                    }
                }

                string OwningTransactionLocalId { get; set; } = string.Empty;
                IResourceGuard Guard { get; }
            }
        }
    }
}
