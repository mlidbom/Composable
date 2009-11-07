using System;
using System.Collections;
using System.Linq;

namespace Void.Data.ORM.InMemory
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