using System;
using System.Transactions;

namespace Composable.Testing.Threading
{
    class TransactionSnapshot
    {
        public TransactionSnapshot(Transaction transaction)
        {
            IsolationLevel = transaction.IsolationLevel;
            TransactionInformation = new TransactionInformationSnapshot(transaction.TransactionInformation);
        }

        public class TransactionInformationSnapshot
        {
            public TransactionInformationSnapshot(TransactionInformation information)
            {
                LocalIdentifier = information.LocalIdentifier;
                DistributedIdentifier = information.DistributedIdentifier;
                Status = information.Status;
            }

            public string LocalIdentifier { get; }
            public Guid DistributedIdentifier { get; }
            public TransactionStatus Status { get; }
        }

        public IsolationLevel IsolationLevel { get; }

        public TransactionInformationSnapshot TransactionInformation { get; }

        public static TransactionSnapshot? TakeSnapshot()
        {
            var currentTransaction = Transaction.Current;
            if(currentTransaction == null)
            {
                return null;
            }

            return new TransactionSnapshot(currentTransaction);
        }
    }
}
