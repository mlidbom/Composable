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


        public IComponentLease<TComponent> Lease<TComponent>() => new WindsorComponentLease<TComponent>(_container.Resolve<TComponent>(), _container);
        public IMultiComponentLease<TComponent> LeaseAll<TComponent>() => new WindsorMultiComponentLease<TComponent>(_container.ResolveAll<TComponent>().ToArray(), _container);
    }
}