using System;
using System.Collections;

namespace Composable.Data.ORM.InMemoryRepositories
{
    public class GuidIdManager<TInstance> : IdManager<TInstance, Guid>
    {
        public override object NextId(IEnumerable allInstancesOfType)
        {
            return Guid.NewGuid();
        }
    }
}