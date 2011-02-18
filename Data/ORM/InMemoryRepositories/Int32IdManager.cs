#region usings

using System.Collections;
using System.Diagnostics.Contracts;
using System.Linq;

#endregion

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
            Contract.Requires(allInstancesOfType != null);
            if (!allInstancesOfType.OfType<TInstance>().Any())
            {
                return 1;
            }
            return allInstancesOfType.Cast<TInstance>().Max(me => (int) Get(me)) + 1;
        }
    }
}