#region usings

using System;
using System.Transactions;
using JetBrains.Annotations;
using NServiceBus;

#endregion

namespace Composable.CQRS.Testing
{
#pragma warning disable 618
    [UsedImplicitly, Obsolete("Nservicebus will soon stop supporting the way this is implemented so this class is going away. Stay away...")]
    public class TestingSupportMessageModule : IMessageModule
#pragma warning restore 618
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