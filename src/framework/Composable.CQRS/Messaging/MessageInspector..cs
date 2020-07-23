using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.LinqCE;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(MessageTypeInspector.AssertValid);

        internal static void AssertValid<TMessage>() => MessageTypeInspector.AssertValid(typeof(TMessage));

        internal static void AssertValidToSendRemote(MessageTypes.IMessage message)
        {
            CommonAssertions(message);

            if(message is MessageTypes.StrictlyLocal.IMessage strictlyLocalMessage) throw new AttemptToSendStrictlyLocalMessageRemotely(strictlyLocalMessage);
            if(message is MessageTypes.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(MessageTypes.IRequireTransactionalSender).FullName} but there is no transaction.");
            if(message is MessageTypes.IForbidTransactionalRemoteSender && Transaction.Current != null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(MessageTypes.IForbidTransactionalRemoteSender).FullName} but there is a transaction.");
            if(message is MessageTypes.Remotable.IAtMostOnceMessage atMostOnce && atMostOnce.MessageId == Guid.Empty) throw new Exception($"{nameof(MessageTypes.Remotable.IAtMostOnceMessage.MessageId)} was Guid.Empty for message of type: {message.GetType().FullName}");
        }

        internal static void AssertValidToExecuteLocally(MessageTypes.IMessage message)
        {
            CommonAssertions(message);

            if(message is MessageTypes.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(MessageTypes.IRequireTransactionalSender).FullName} but there is no transaction.");
        }

        static void CommonAssertions(MessageTypes.IMessage message)
        {
            MessageTypeInspector.AssertValid(message.GetType());
            if(message is MessageTypes.ICommand command) CommandValidator.AssertCommandIsValid(command);
        }
    }
}
