using System;

namespace Composable.KeyValueStorage.Population
{
    public interface IViewModelBatchPopulator
    {
        void PopulateEntities(params Guid[] aggregateRootIds);
    }
}