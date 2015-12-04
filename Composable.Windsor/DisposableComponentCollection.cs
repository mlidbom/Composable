using System;
using System.Collections.Generic;
using Castle.Windsor;
using Composable.System.Linq;

namespace Composable.Windsor
{
    public class DisposableComponentCollection<T> : IDisposable
    {
        private readonly IWindsorContainer _container;

        public DisposableComponentCollection(IEnumerable<T> instances, IWindsorContainer container)
        {
            _container = container;
            Instances = instances;
        }

        public IEnumerable<T> Instances { get; private set; }
        public void Dispose()
        {
            Instances.ForEach(instance => _container.Release(instance));
        }
    }
}