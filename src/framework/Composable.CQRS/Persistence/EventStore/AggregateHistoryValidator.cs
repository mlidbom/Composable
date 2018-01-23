using System;
using System.Collections.Generic;
// ReSharper disable LoopCanBeConvertedToQuery

namespace Composable.Persistence.EventStore
{
    static class AggregateHistoryValidator
    {
        public static void ValidateHistory(Guid aggregateId, IReadOnlyList<IAggregateEvent> history)
        {
            int version = 1;
            foreach(var aggregateRootEvent in history)
            {
                if(aggregateRootEvent.AggregateVersion != version++)
                {
                    throw new InvalidHistoryException(aggregateId);
                }
            }
        }
    }
}