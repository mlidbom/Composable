using System;
using Castle.Windsor;

namespace Composable.CQRS.Windsor
{
    [Obsolete("'Now in the Composable.Windsor namespace. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public class DisposableComponent<T> : IDisposable
    {
        private readonly IWindsorContainer _container;

        public DisposableComponent(T component, IWindsorContainer container)
        {
            _container = container;
            Instance = component;
        }

        public T Instance { get; private set; }
        public void Dispose()
        {
            _container.Release(Instance);
        }
    }
}