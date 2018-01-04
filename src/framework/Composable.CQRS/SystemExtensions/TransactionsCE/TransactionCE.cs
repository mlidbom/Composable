using System;
using System.Transactions;

namespace Composable.SystemExtensions.TransactionsCE
{
    public static class TransactionCE
    {
        public static void OnCommit(this Transaction @this, Action action)
        {
            @this.TransactionCompleted += (sender, args) =>
            {
                if(args.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
                {
                    action();
                }
            };
        }
    }
}
