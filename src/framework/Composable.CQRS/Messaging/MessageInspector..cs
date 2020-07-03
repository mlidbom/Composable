using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System.Linq;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(MessageTypeInspector.AssertValid);

        internal static void AssertValid<TMessage>() => MessageTypeInspector.AssertValid(typeof(TMessage));

        internal static void AssertValidToSendRemote(MessageTypes.IMessage message)
        {
            if(message is MessageTypes.StrictlyLocal.IMessage strictlyLocalMessage) throw new AttemptToSendStrictlyLocalMessageRemotely(strictlyLocalMessage);
            if(message is MessageTypes.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(MessageTypes.IRequireTransactionalSender).FullName} but there is no transaction.");
            if(message is MessageTypes.IForbidTransactionalRemoteSender && Transaction.Current != null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(MessageTypes.IForbidTransactionalRemoteSender).FullName} but there is a transaction.");
            if(message is MessageTypes.Remotable.IAtMostOnceMessage atMostOnce && atMostOnce.MessageId == Guid.Empty) throw new Exception($"{nameof(MessageTypes.Remotable.IAtMostOnceMessage.MessageId)} was Guid.Empty for message of type: {message.GetType().FullName}");

            MessageTypeInspector.AssertValid(message.GetType());
        }

        internal static void AssertValidToSendLocal(MessageTypes.IMessage message)
        {
            if(message is MessageTypes.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(MessageTypes.IRequireTransactionalSender).FullName} but there is no transaction.");

            MessageTypeInspector.AssertValid(message.GetType());
        }
    }
}
