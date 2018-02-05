using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        static readonly HashSet<Type> SuccessfullyInspectedTypes = new HashSet<Type>();

        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(AssertValid);

        internal static void AssertValid<TMessage>() => AssertValid(typeof(TMessage));

        internal static void AssertValidToSendRemote(BusApi.IMessage message)
        {
            if(message is BusApi.StrictlyLocal.IMessage strictlyLocalMessage) throw new AttemptToSendStrictlyLocalMessageRemotely(strictlyLocalMessage);
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IRequireTransactionalSender).FullName} but there is no transaction.");
            if(message is BusApi.IForbidTransactionalRemoteSender && Transaction.Current != null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IForbidTransactionalRemoteSender).FullName} but there is a transaction.");
            if(message is BusApi.Remotable.IAtMostOnceMessage atMostOnce && atMostOnce.MessageId == Guid.Empty) throw new Exception($"Message id was Guid.Empty for message of type: {message.GetType().FullName}");

            AssertValid(message.GetType());
        }

        internal static void AssertValidToSendLocal(BusApi.IMessage message)
        {
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IRequireTransactionalSender).FullName} but there is no transaction.");

            AssertValid(message.GetType());
        }

        static void AssertValid(Type type)
        {
            lock(SuccessfullyInspectedTypes)
            {
                if(SuccessfullyInspectedTypes.Contains(type)) return;

                if(!type.Implements<BusApi.IMessage>()) throw new Exception($"{type.FullName} is not an {nameof(BusApi.IMessage)}");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IEvent>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.ICommand).FullName} and {typeof(BusApi.IEvent).FullName}.");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IQuery>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.ICommand).FullName} and {typeof(BusApi.IQuery).FullName}.");
                if(type.Implements<BusApi.IEvent>() && type.Implements<BusApi.IQuery>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.IEvent).FullName} and {typeof(BusApi.IQuery).FullName}.");
                if(type.Implements<BusApi.Remotable.IMessage>() && type.Implements<BusApi.StrictlyLocal.IMessage>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.Remotable.IMessage).FullName} and {typeof(BusApi.StrictlyLocal.IMessage).FullName}.");
                if(type.Implements<BusApi.IRequireTransactionalSender>() && type.Implements<BusApi.IForbidTransactionalRemoteSender>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.IRequireTransactionalSender).FullName} and {typeof(BusApi.IForbidTransactionalRemoteSender).FullName}.");
                if(type.Implements<BusApi.IQuery>() && !type.IsAbstract && !type.Implements(typeof(BusApi.IQuery<>))) throw new MessageTypeDesignViolationException($"{type.FullName} implements only: {typeof(BusApi.IQuery).FullName}. Concrete types must implement {typeof(BusApi.IQuery<>).GetFullNameCompilable()}");

                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }
}
