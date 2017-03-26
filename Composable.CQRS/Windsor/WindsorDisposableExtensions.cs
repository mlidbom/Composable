using System;
using Castle.Windsor;

namespace Composable.Windsor
{
    static class WindsorDisposableExtensions
    {
        internal static void UseComponent<TComponent>(this IWindsorContainer me, string componentName, Action<TComponent> action)
        {
            using (var component = new WindsorComponentLease<TComponent>(me.Resolve<TComponent>(componentName), me))
            {
                action(component.Instance);
            }
        }
    }
}
