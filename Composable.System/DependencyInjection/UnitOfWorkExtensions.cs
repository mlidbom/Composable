using System;
using System.Runtime.Remoting.Messaging;
using System.Transactions;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class PublicUnitOfWorkExtensions
    {
        public static TResult ExecuteUnitOfWork<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            TResult result;
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                result = function();
                transaction.Complete();
            }
            return result;
        }

        public static void ExecuteUnitOfWork(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Complete();
            }
        }

    }

    static class UnitOfWorkExtensions
    {
        static TResult ExecuteUnitOfWork<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            TResult result;
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                result = function();
                transaction.Complete();
            }
            return result;
        }

        static void ExecuteUnitOfWork(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Complete();
            }
        }

        internal static TResult ExecuteUnitOfWorkInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return ExecuteUnitOfWork(me, function);
            }
        }

        internal static void ExecuteUnitOfWorkInIsolatedScope(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (me.BeginScope())
            {
                ExecuteUnitOfWork(me, action);
            }
        }

        internal static TResult ExecuteInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return function();
            }
        }

        internal static void ExecuteInIsolatedScope(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (me.BeginScope())
            {
                action();
            }
        }


        public static TransactionScope BeginTransactionalUnitOfWorkScope(this IServiceLocator @this)
        {
            return new TransactionScope();
        }
    }
}