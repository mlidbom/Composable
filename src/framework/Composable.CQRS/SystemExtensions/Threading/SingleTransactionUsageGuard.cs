using System.Transactions;
using Composable.Logging;

namespace Composable.SystemExtensions.Threading {
    class SingleTransactionUsageGuard : ISingleContextUseGuard
    {
        ILogger _log = Logger.For<SingleTransactionUsageGuard>();

        Transaction _transaction;
        public SingleTransactionUsageGuard() => _transaction = Transaction.Current;

        public void AssertNoContextChangeOccurred(object guarded)
        {
            _transaction = _transaction ?? Transaction.Current;
            if(Transaction.Current != null && Transaction.Current != _transaction)
            {
                _log.Warning($"Using the {guarded.GetType()} in multiple transactions is not safe. It makes you vulnerable to hard to debug concurrency issues and is therefore not allowed.");
                //throw new EventStoreUpdaterUsedFromMultipleTransactionsException(); //Todo: Switch to throwing an exception here.
            }
        }
    }
}