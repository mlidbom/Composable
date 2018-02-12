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

        internal static void AssertValidToSendRemote(BusApi.IMessage message)
        {
            if(message is BusApi.StrictlyLocal.IMessage strictlyLocalMessage) throw new AttemptToSendStrictlyLocalMessageRemotely(strictlyLocalMessage);
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IRequireTransactionalSender).FullName} but there is no transaction.");
            if(message is BusApi.IForbidTransactionalRemoteSender && Transaction.Current != null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IForbidTransactionalRemoteSender).FullName} but there is a transaction.");
            if(message is BusApi.Remotable.IAtMostOnceMessage atMostOnce && atMostOnce.DeduplicationId == Guid.Empty) throw new Exception($"{nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)} was Guid.Empty for message of type: {message.GetType().FullName}");

            MessageTypeInspector.AssertValid(message.GetType());
        }

        internal static void AssertValidToSendLocal(BusApi.IMessage message)
        {
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IRequireTransactionalSender).FullName} but there is no transaction.");

            MessageTypeInspector.AssertValid(message.GetType());
        }
    }
}
