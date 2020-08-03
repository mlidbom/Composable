using System;
using System.Collections.Generic;
using Composable.SystemCE.LinqCE;

namespace Composable.SystemCE
{
    static class ActionCE
    {
        internal static void InvokeAll(this IEnumerable<Action> @this) => @this.ForEach(me => me.Invoke());
    }
}
