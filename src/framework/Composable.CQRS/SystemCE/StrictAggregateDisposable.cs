using System;
using System.Collections.Generic;
using Composable.SystemCE.Collections;

namespace Composable.SystemCE
{    public class StrictAggregateDisposable : StrictlyManagedResourceBase<StrictAggregateDisposable>
    {
        readonly IList<IDisposable> _managedResources = new List<IDisposable>();

        public static StrictAggregateDisposable Create(params IDisposable[] disposables) => new StrictAggregateDisposable(disposables);

        StrictAggregateDisposable(params IDisposable[] disposables)
        {
            Add(disposables);
        }

        internal void Add(params IDisposable[] disposables) { _managedResources.AddRange(disposables); }

        protected override void Dispose(bool disposing)
        {
            foreach (var managedResource in _managedResources)
            {
                managedResource.Dispose();
            }
            _managedResources.Clear();
            base.Dispose(disposing);
        }
    }
}