using System;
using System.Collections.Generic;
using Castle.Windsor;

namespace Composable.Windsor
{
    public static class WindsorDisposableExtensions
    {
        public static DisposableComponent<TComponent> ResolveDisposable<TComponent>(this IWindsorContainer me)
        {
            return new DisposableComponent<TComponent>(me.Resolve<TComponent>(), me);
        }

        public static DisposableComponentCollection<TComponent> ResolveAllDisposable<TComponent>(this IWindsorContainer me)
        {
            return new DisposableComponentCollection<TComponent>(me.ResolveAll<TComponent>(), me);
        }

        public static void UseComponent<TComponent>(this IWindsorContainer me, Action<TComponent> action )
        {
            using(var component = new DisposableComponent<TComponent>(me.Resolve<TComponent>(), me))
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

        public static void UseComponents<TComponent>(this IWindsorContainer me, Action<IEnumerable<TComponent>> action)
        {
            using (var component = new DisposableComponentCollection<TComponent>(me.ResolveAll<TComponent>(), me))
            {
                action(component.Instances);
            }
        }
    }
}
