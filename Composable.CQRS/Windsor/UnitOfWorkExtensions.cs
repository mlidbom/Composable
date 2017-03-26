using System;
using Composable.DependencyInjection;
using JetBrains.Annotations;

namespace Composable.Windsor
{
    public static class UnitOfWorkExtensions
    {
        public static TResult ExecuteUnitOfWork<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            TResult result;
            using (var transaction = me.Unsupported().BeginTransactionalUnitOfWorkScope())
            {
                result = function();
                transaction.Commit();
            }
            return result;
        }

        public static void ExecuteUnitOfWork(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (var transaction = me.Unsupported().BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Commit();
            }
        }
    }
}