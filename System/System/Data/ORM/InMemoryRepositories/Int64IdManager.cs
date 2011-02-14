using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Composable.Data.ORM.InMemoryRepositories
{
    public class Int64IdManager<TInstance> : IdManager<TInstance, long>
    {
        public Int64IdManager()
        {
            Unsaved = (long)0;
        }

        public override object NextId(IEnumerable allInstancesOfType)
        {
            Contract.Requires(allInstancesOfType != null);
            if (!allInstancesOfType.OfType<TInstance>().Any())
            {
                return (long)1;
            }
            return allInstancesOfType.Cast<TInstance>().Max(me => (long)Get(me)) + 1;
        }
    }
}