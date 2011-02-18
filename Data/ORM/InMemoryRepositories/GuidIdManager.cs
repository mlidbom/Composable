#region usings

using System;
using System.Collections;

#endregion

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