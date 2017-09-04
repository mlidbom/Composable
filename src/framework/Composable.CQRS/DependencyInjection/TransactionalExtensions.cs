using System;
using Composable.System.Transactions;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class ServiceLocatorTransactionRunner
    {
        public static TResult ExecuteTransaction<TResult>(this IServiceLocator ignored, [InstantHandle]Func<TResult> function)
        {
            return TransactionScopeCe.Execute(function);
        }

        public static void ExecuteTransaction(this IServiceLocator ignored, [InstantHandle]Action action)
        {
            TransactionScopeCe.Execute(action);
        }

        internal static TResult ExecuteTransactionInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return TransactionScopeCe.Execute(function);
            }
        }

        internal static void ExecuteTransactionInIsolatedScope(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (me.BeginScope())
            {
                TransactionScopeCe.Execute(action);
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
    }
}