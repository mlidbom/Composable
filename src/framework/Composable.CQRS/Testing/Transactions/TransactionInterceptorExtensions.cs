using System;
using System.Transactions;

namespace Composable.Testing.Transactions
{
    static class TransactionInterceptorExtensions
    {
        public static void FailOnPrepare(this Transaction @this, Exception? exception = null) =>
            @this.Intercept(onPrepare: enlistment => enlistment.ForceRollback(exception ?? new Exception($"{nameof(TransactionInterceptorExtensions)}.{nameof(FailOnPrepare)}")));

        public static TransactionInterceptor Intercept(this Transaction @this,
                                                       Action<PreparingEnlistment>? onPrepare = null,
                                                       Action<Enlistment>? onCommit = null,
                                                       Action<Enlistment>? onRollback = null,
                                                       Action<Enlistment>? onInDoubt = null)
            => new TransactionInterceptor(@this, onPrepare, onCommit, onRollback, onInDoubt);
    }
}
