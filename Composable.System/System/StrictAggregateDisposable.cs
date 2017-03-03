using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Collections.Collections;

namespace Composable.System
{
    public interface IAggregateDisposable : IDisposable
    {
        void Add(params IDisposable[] disposables);
        void Add(IEnumerable<IDisposable> disposables);
    }

    public interface IStrictAggregateDisposable : IAggregateDisposable, IStrictlyManagedResource
    {}

    public class StrictAggregateDisposable : StrictlyManagedResourceBase<StrictAggregateDisposable>, IStrictAggregateDisposable
    {
        readonly IList<IDisposable> _managedResources = new List<IDisposable>();

        public static StrictAggregateDisposable Create(params IDisposable[] disposables)
        {
            return new StrictAggregateDisposable(disposables);
        }

        public static StrictAggregateDisposable Create(IEnumerable<IDisposable> disposables)
        {
            return new StrictAggregateDisposable(disposables);
        }

        public StrictAggregateDisposable() { }

        public StrictAggregateDisposable(params IDisposable[] disposables)
        {
            Add(disposables);
        }

        public StrictAggregateDisposable(IEnumerable<IDisposable> disposables)
        {
            Add(disposables.ToArray());
        }

        public void Add(params IDisposable[] disposables) => _managedResources.AddRange(disposables);

        public void Add(IEnumerable<IDisposable> disposables) => _managedResources.AddRange(disposables);

        protected override void InternalDispose()
        {
            foreach (var managedResource in _managedResources)
            {
                managedResource.Dispose();
            }
            _managedResources.Clear();
        }
    }
}