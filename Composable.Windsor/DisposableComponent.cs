using System;
using Castle.Windsor;

namespace Composable.Windsor
{
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