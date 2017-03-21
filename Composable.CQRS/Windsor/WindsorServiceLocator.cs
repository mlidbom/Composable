using System;
using System.Linq;
using Castle.Windsor;
using Composable.DependencyInjection;

namespace Composable.Windsor
{
    static class WindsorServiceLocatorExtensions
    {
        internal static IServiceLocator AsServiceLocator(this IWindsorContainer @this) => new WindsorServiceLocator(@this);
    }

    class WindsorServiceLocator : IServiceLocator
    {
        readonly IWindsorContainer _container;
        public WindsorServiceLocator(IWindsorContainer container) { _container = container; }

        public IComponentLease<object> Lease(Type componentType) => new WindsorComponentLease<object>(_container.Resolve(componentType), _container);
        public IMultiComponentLease<object> LeaseAll(Type componentType) => new WindsorMultiComponentLease<object>(_container.ResolveAll(componentType).Cast<object>().ToArray(), _container);
    }
}