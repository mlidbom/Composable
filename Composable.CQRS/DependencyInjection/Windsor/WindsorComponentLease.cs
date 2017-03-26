using Castle.Windsor;
using Composable.System.Linq;

namespace Composable.DependencyInjection.Windsor
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

    class WindsorMultiComponentLease<T> : IMultiComponentLease<T>
    {
        readonly IWindsorContainer _container;

        public WindsorMultiComponentLease(T[] components, IWindsorContainer container)
        {
            _container = container;
            Instances = components;
        }

        public T[] Instances { get; }
        public void Dispose() => Instances.ForEach(instance => _container.Release(instance));
    }
}