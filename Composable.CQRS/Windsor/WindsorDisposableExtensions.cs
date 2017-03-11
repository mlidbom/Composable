using System;
using Castle.Windsor;

namespace Composable.CQRS.Windsor
{
    static class WindsorDisposableExtensions
    {
        public static void UseComponent<TComponent>(this IWindsorContainer me, string componentName, Action<TComponent> action)
        {
            using (var component = new WindsorComponentLease<TComponent>(me.Resolve<TComponent>(componentName), me))
            {
                action(component.Instance);
            }
        }
    }
}
