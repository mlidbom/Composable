using System;

namespace Composable.KeyValueStorage.Population
{
    public interface IPopulationBatchLogger
    {
        void LogAggregateHandled(Guid entityId);
        void LogError(Exception e, Guid entityId);
        void Initialize(int numberOfAggregates);
    }
}