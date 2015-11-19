using System;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;

namespace Composable.CQRS.Windsor
{
    [Obsolete("'Now in the Composable.Windsor namespace. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public static class WindsorUnitOfWorkExtensions
    {
        public static TResult ExecuteUnitOfWork<TResult>(this IWindsorContainer me, Func<TResult> function)
        {
            TResult result;
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                result = function();
                transaction.Commit();
            }
            return result;
        }

        public static void ExecuteUnitOfWork(this IWindsorContainer me, Action action)
        {
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Commit();
            }
        }

        public static TResult ExecuteUnitOfWorkInIsolatedScope<TResult>(this IWindsorContainer me, Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return me.ExecuteUnitOfWork(function);
            }
        }

        public static void ExecuteUnitOfWorkInIsolatedScope(this IWindsorContainer me, Action action)
        {
            using (me.BeginScope())
            {
                me.ExecuteUnitOfWork(action);
            }
        }

        public static TResult ExecuteInIsolatedScope<TResult>(this IWindsorContainer me, Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return function();
            }
        }

        public static void ExecuteInIsolatedScope(this IWindsorContainer me, Action action)
        {
            using (me.BeginScope())
            {
                action();
            }
        }
    }
}
