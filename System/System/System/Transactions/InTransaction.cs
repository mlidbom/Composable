using System;
using System.Diagnostics.Contracts;
using System.Transactions;

namespace Composable.System.Transactions
{
    ///<summary>Simple utility class for executing a<see cref="Action"/> within a <see cref="TransactionScope"/></summary>
    public static class InTransaction
    {
        ///<summary>Runs the supplied action within a <see cref="TransactionScope"/></summary>
        public static void Execute(Action action)
        {
            Contract.Requires(action != null);
            using(var transaction = new TransactionScope())
            {
                action();
                transaction.Complete();
            }
        }
    }
}