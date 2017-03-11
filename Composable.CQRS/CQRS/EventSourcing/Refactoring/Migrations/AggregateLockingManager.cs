using System;
using System.Collections.Generic;
using Composable.System.Collections.Collections;

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations
{
    static class AggregateLockManager
    {
        static readonly Dictionary<Guid, object> AggregateHistoryLockObjects = new Dictionary<Guid, object>();
        public static object GetAggregateLockObject(Guid aggregateId)
        {
            lock (AggregateHistoryLockObjects)
            {
                return AggregateHistoryLockObjects.GetOrAdd(aggregateId, () => new object());
            }
        }
    }
}