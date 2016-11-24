using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Transactions;

namespace Composable.System.Transactions
{
    ///<summary>Extensions for working with transactions.</summary>
    public static class TransactionExtensions
    {
        ///<summary>Blocks the current thread until the transaction completes or the timeout expires.</summary>
         public static bool WaitForTransactionToComplete(this Transaction me, TimeSpan timeout)
         {   
             Contract.Requires(me != null);

             var done = new ManualResetEventSlim();
             me.TransactionCompleted += (_, __) => done.Set();

             if(me.TransactionInformation.Status != TransactionStatus.Active)
             {
                 done.Set();
             }

             return done.Wait(timeout);
         }
    }
}