using System;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
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
        internal readonly IWindsorContainer WindsorContainer;
        public WindsorServiceLocator(IWindsorContainer container) => WindsorContainer = container;

        public IComponentLease<TComponent> Lease<TComponent>() => new WindsorComponentLease<TComponent>(WindsorContainer.Resolve<TComponent>(), WindsorContainer);
        public IMultiComponentLease<TComponent> LeaseAll<TComponent>() => new WindsorMultiComponentLease<TComponent>(WindsorContainer.ResolveAll<TComponent>().ToArray(), WindsorContainer);
        public IDisposable BeginScope() => WindsorContainer.BeginScope();
        public void Dispose() => WindsorContainer.Dispose();
    }
}