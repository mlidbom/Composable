using System;
using System.Transactions;

namespace Composable.SystemCE.TransactionsCE
{
    static class TransactionCE
    {
        internal static void OnCommittedSuccessfully(this Transaction @this, Action action)
        {
            @this.TransactionCompleted += (sender, args) =>
            {
                if(args.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
                {
                    action();
                }
            };
        }

        internal static void OnCompleted(this Transaction @this, Action action) => @this.TransactionCompleted += (sender, args) => action();

        internal static void OnAbort(this Transaction @this, Action action)
        {
            @this.TransactionCompleted += (sender, args) =>
            {
                if(args.Transaction.TransactionInformation.Status == TransactionStatus.Aborted)
                {
                    action();
                }
            };
        }
    }
}
