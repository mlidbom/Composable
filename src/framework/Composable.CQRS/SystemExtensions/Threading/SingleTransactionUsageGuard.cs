using System.Transactions;

namespace Composable.SystemExtensions.Threading
{
    class SingleTransactionUsageGuard : ISingleContextUseGuard
    {
        Transaction _transaction;
        public SingleTransactionUsageGuard() => _transaction = Transaction.Current;

        public void AssertNoContextChangeOccurred(object guarded)
        {
            _transaction = _transaction ?? Transaction.Current;
            if(Transaction.Current != null && Transaction.Current != _transaction)
            {
                throw new ComponentUsedByMultipleTransactionsException(guarded.GetType());
            }
        }
    }
}
