using System;
using System.Collections.Generic;
using Composable.Persistence.EventStore;

static internal class AggregateHistoryValidator {
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