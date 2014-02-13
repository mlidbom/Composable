using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Windsor;

namespace Composable.CQRS.Windsor
{
    public static class WindsorDisposableExtensions
    {
        public static DisposableComponent<TComponent> ResolveDisposable<TComponent>(this IWindsorContainer me)
        {
            return new DisposableComponent<TComponent>(me.Resolve<TComponent>(), me);
        }
    }
}
