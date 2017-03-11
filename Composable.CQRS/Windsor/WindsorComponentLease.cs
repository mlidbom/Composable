using Castle.Windsor;
using Composable.DependencyInjection;

namespace Composable.CQRS.Windsor
{
    class WindsorComponentLease<T> : IComponentLease<T>
    {
        readonly IWindsorContainer _container;

        public WindsorComponentLease(T component, IWindsorContainer container)
        {
            _container = container;
            Instance = component;
        }

        public T Instance { get; private set; }
        public void Dispose() => _container.Release(Instance);
    }
}