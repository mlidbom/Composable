using System;
using System.Collections.Generic;
// ReSharper disable LoopCanBeConvertedToQuery

namespace Composable.Persistence.EventStore
{
    static class AggregateHistoryValidator
    {
        public static void ValidateHistory(Guid aggregateId, IReadOnlyList<IAggregateRootEvent> history)
        {
            int version = 1;
            foreach(var aggregateRootEvent in history)
            {
                if(aggregateRootEvent.AggregateRootVersion != version++)
                {
                    throw new InvalidHistoryException(aggregateId);
                }
            }
        }
    }
}