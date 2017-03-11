using System;

using System.Transactions;
using Composable.Contracts;
using JetBrains.Annotations;

namespace Composable.System.Transactions
{
    ///<summary>Simple utility class for executing a<see cref="Action"/> within a <see cref="TransactionScope"/></summary>
    public static class InTransaction
    {
        ///<summary>Runs the supplied action within a <see cref="TransactionScope"/></summary>
        public static void Execute([InstantHandle]Action action)
        {
            ContractOptimized.Argument(action, nameof(action))
                             .NotNull();

            using(var transaction = new TransactionScope())
            {
                action();
                transaction.Complete();
            }
        }
    }
}