using System;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;

namespace Composable.CQRS.Windsor
{
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

        internal static TResult ExecuteUnitOfWorkInIsolatedScope<TResult>(this IWindsorContainer me, Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return me.ExecuteUnitOfWork(function);
            }
        }

        internal static void ExecuteUnitOfWorkInIsolatedScope(this IWindsorContainer me, Action action)
        {
            using (me.BeginScope())
            {
                me.ExecuteUnitOfWork(action);
            }
        }

        internal static TResult ExecuteInIsolatedScope<TResult>(this IWindsorContainer me, Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return function();
            }
        }

        internal static void ExecuteInIsolatedScope(this IWindsorContainer me, Action action)
        {
            using (me.BeginScope())
            {
                action();
            }
        }
    }
}
