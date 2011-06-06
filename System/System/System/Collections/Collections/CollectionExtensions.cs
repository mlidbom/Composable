using System;
using System.Collections.Generic;
using Composable.System.Linq;
using System.Linq;

namespace Composable.System.Collections.Collections
{
    public static class CollectionExtensions
    {
         public static int RemoveWhere<T>(this ICollection<T> me, Func<T, bool> condition )
         {
             var toRemove = me.Where(condition).ToList();
             toRemove.ForEach(removeMe => me.Remove(removeMe));
             return toRemove.Count;
         }
    }
}