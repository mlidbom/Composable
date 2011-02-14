using System;
using System.Collections;
using System.Linq;

namespace Composable.Data.ORM.InMemoryRepositories
{
    public class Int32IdManager<TInstance> : IdManager<TInstance, int>
    {
        public Int32IdManager()
        {
            Unsaved = 0;
        }
        public override object NextId(IEnumerable allInstancesOfType)
        {
            if(!allInstancesOfType.OfType<TInstance>().Any())
            {
                return 1;
            }
            return allInstancesOfType.Cast<TInstance>().Max(me => (int)Get(me)) + 1;
        }
    }
}