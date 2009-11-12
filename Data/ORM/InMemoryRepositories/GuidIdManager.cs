using System;
using System.Collections;

namespace Void.Data.ORM.InMemory
{
    public class GuidIdManager<TInstance> : IdManager<TInstance, Guid>
    {
        public override object NextId(IEnumerable allInstancesOfType)
        {
            return Guid.NewGuid();
        }
    }
}