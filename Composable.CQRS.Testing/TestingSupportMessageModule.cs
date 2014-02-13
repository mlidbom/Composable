#region usings

using System;
using System.Transactions;
using NServiceBus;

#endregion

namespace Composable.CQRS.Testing
{
    public class TestingSupportMessageModule : IMessageModule
    {
        public static event Action<Transaction> OnHandleBeginMessage = t => {};
        public void HandleBeginMessage()
        {
            OnHandleBeginMessage(Transaction.Current);
        }

        public static event Action<Transaction> OnHandleEndMessage = t => { };
        public void HandleEndMessage()
        {
            OnHandleEndMessage(Transaction.Current);
        }

        public static event Action OnHandleError = () => { };
        public void HandleError()
        {
            OnHandleError();
        }
    }
}