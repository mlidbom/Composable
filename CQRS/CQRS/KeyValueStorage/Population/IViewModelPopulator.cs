using System;

namespace Composable.KeyValueStorage.Population
{
    public interface IViewModelPopulator
    {
        void Populate(Guid entityId);
    }
}