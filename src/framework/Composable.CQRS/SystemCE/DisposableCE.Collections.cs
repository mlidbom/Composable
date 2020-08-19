using System;
using System.Collections.Generic;
using Composable.SystemCE.LinqCE;

namespace Composable.SystemCE
{
    static class DisposableCECollections
    {
        internal static void DisposeAll(this IEnumerable<IDisposable> disposables) => disposables.ForEach(disposable => disposable.Dispose());
    }
}
