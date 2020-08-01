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

        internal static IDisposable NoTransactionEscalationScope(string scopeDescription)
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier ?? Guid.Empty;

            return DisposableCE.Create(() =>
            {
                if(Transaction.Current != null && transactionInformationDistributedIdentifierBefore == Guid.Empty && Transaction.Current!.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception($"{scopeDescription} escalated transaction to distributed. For now this is disallowed");
                }
            });
        }
    }
}
