using System;
using System.Threading;
using System.Transactions;

namespace Composable.System.Transactions
{
    public static class TransactionExtensions
    {
         public static bool WaitForTransactionToComplete(this Transaction me, TimeSpan timeout)
         {             
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