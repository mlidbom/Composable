﻿using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading.ResourceAccess;
using Composable.SystemExtensions.TransactionsCE;

namespace Composable.Persistence.InMemory.EventStore
{
    partial class InMemoryEventStorePersistenceLayer
    {
        class TransactionLockManager
        {
            readonly Dictionary<Guid, TransactionWideLock> _aggregateGuards = new Dictionary<Guid, TransactionWideLock>();

            public TResult WithExclusiveAccess<TResult>(Guid aggregateId, Func<TResult> func) => WithExclusiveAccess(aggregateId, true, func);
            public TResult WithExclusiveAccess<TResult>(Guid aggregateId, bool takeWriteLock, Func<TResult> func)
            {
                if(Transaction.Current != null)
                {
                    var @lock = _aggregateGuards.GetOrAdd(aggregateId, () => new TransactionWideLock());
                    @lock.AwaitAccess(takeWriteLock);
                }

                return func();
            }

            public void WithExclusiveAccess(Guid aggregateId, Action action) => WithExclusiveAccess(aggregateId, true, () =>
            {
                action();
                return 1;
            });

            class TransactionWideLock
            {
                public TransactionWideLock() => Guard = ResourceGuard.WithTimeout(1.Minutes());

                public void AwaitAccess(bool takeWriteLock)
                {
                    if(OwningTransactionLocalId == string.Empty && !takeWriteLock)
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