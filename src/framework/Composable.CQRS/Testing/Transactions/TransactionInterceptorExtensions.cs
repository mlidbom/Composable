using System;
using System.Transactions;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Testing.Transactions
{
    static class TransactionInterceptorExtensions
    {
        public static void FailOnPrepare(this Transaction @this, Exception? exception = null) =>
            @this.AddPrepareTasks(() =>
            {
                if(exception != null) throw exception;
                else throw new Exception($"{nameof(TransactionInterceptorExtensions)}.{nameof(FailOnPrepare)}");
            });
    }
}
