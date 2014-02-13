using System;
using JetBrains.Annotations;

namespace Composable.KeyValueStorage.Population
{
    [UsedImplicitly]
    public class NullOperationLogger : IPopulationBatchLogger {
        public void LogAggregateHandled(Guid aggregateId)
        {            
        }

        public void LogError(Exception e, Guid entityId)
        {
        }

        public void Initialize(int numberOfAggregates)
        {
        }
    }
}