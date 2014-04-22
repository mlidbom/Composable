using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Transactions;

namespace Composable.System.Transactions
{
    public static class TransactionExtensions
    {
         public static bool WaitForTransactionToComplete(this Transaction me, TimeSpan timeout)
         {   
             Contract.Requires(me != null);

             var done = new ManualResetEvent(false);
             me.TransactionCompleted += (_, __) => done.Set();

             if(me.TransactionInformation.Status != TransactionStatus.Active)
             {
                 done.Set();
             }

             return done.WaitOne(timeout);
         }
    }
}