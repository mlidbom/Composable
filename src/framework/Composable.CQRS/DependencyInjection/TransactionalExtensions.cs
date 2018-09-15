﻿using System;
using System.Threading.Tasks;
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

        public static TResult ExecuteInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return function();
            }
        }

        public static void ExecuteInIsolatedScope(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (me.BeginScope())
            {
                action();
            }
        }

        public static async Task<TResult> ExecuteInIsolatedScopeAsync<TResult>(this IServiceLocator me, [InstantHandle]Func<Task<TResult>> function)
        {
            using (me.BeginScope())
            {
                return await function();
            }
        }

        internal static async Task ExecuteInIsolatedScopeAsync(this IServiceLocator me, [InstantHandle]Func<Task> action)
        {
            using (me.BeginScope())
            {
                await action();
            }
        }
    }
}