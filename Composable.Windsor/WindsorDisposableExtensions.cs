using System;
using Castle.Windsor;

namespace Composable.Windsor
{
    public static class WindsorDisposableExtensions
    {
        public static DisposableComponent<TComponent> ResolveDisposable<TComponent>(this IWindsorContainer me)
        {
            return new DisposableComponent<TComponent>(me.Resolve<TComponent>(), me);
        }

        public static void UseComponent<TComponent>(this IWindsorContainer me, Action<TComponent> action )
        {
            using(var component = me.ResolveDisposable<TComponent>())
            {
                action(component.Instance);
            }
        }

        public static void UseComponent<TComponent>(this IWindsorContainer me, string componentName, Action<TComponent> action)
        {
            using (var component = new DisposableComponent<TComponent>(me.Resolve<TComponent>(componentName), me))
            {
                action(component.Instance);
            }
        }
    }
}
