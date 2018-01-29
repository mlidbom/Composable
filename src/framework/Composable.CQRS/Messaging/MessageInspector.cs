using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Messaging
{
    static class MessageInspector
    {
        static readonly HashSet<Type> SuccessfullyInspectedTypes = new HashSet<Type>();

        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(AssertValid);

        internal static void AssertValid<TMessage>() => AssertValid(typeof(TMessage));

        internal static void AssertValidToSendRemote(BusApi.IMessage message)
        {
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {nameof(BusApi.IRequireTransactionalSender)} but there is no transaction.");
            if(message is BusApi.IForbidTransactionalRemoteSender && Transaction.Current != null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {nameof(BusApi.IForbidTransactionalRemoteSender)} but there is a transaction.");

            AssertValid(message.GetType());
        }

        internal static void AssertValidToSendLocal(BusApi.IMessage message)
        {
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {nameof(BusApi.IRequireTransactionalSender)} but there is no transaction.");

            AssertValid(message.GetType());
        }

        static void AssertValid(Type type)
        {
            lock(SuccessfullyInspectedTypes)
            {
                if(SuccessfullyInspectedTypes.Contains(type)) return;

                if(!type.Implements<BusApi.IMessage>()) throw new Exception($"{type.FullName} is not an {nameof(BusApi.IMessage)}");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IEvent>()) throw new Exception($"{type.FullName} implements both {typeof(BusApi.ICommand)} and {typeof(BusApi.IEvent)}.");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IQuery>()) throw new Exception($"{type.FullName} implements both {typeof(BusApi.ICommand)} and {typeof(BusApi.IQuery)}.");
                if(type.Implements<BusApi.IEvent>() && type.Implements<BusApi.IQuery>()) throw new Exception($"{type.FullName} implements both {typeof(BusApi.IEvent)} and {typeof(BusApi.IQuery)}.");
                if(type.Implements<BusApi.Remotable.IMessage>() && type.Implements<BusApi.StrictlyLocal.IRequireLocalReceiver>()) throw new Exception($"{type.FullName} implements both {typeof(BusApi.Remotable.IMessage)} and {typeof(BusApi.StrictlyLocal.IRequireLocalReceiver)}.");
                if(type.Implements<BusApi.IRequireTransactionalSender>() && type.Implements<BusApi.IForbidTransactionalRemoteSender>()) throw new Exception($"{type.FullName} implements both {typeof(BusApi.IRequireTransactionalSender)} and {typeof(BusApi.IForbidTransactionalRemoteSender)}.");
                if(type.Implements<BusApi.IQuery>() && !type.IsAbstract && !type.Implements(typeof(BusApi.IQuery<>))) throw new Exception($"{type.FullName} implements only: {nameof(BusApi.IQuery)}. Concrete types must implement {typeof(BusApi.IQuery<>).GetFullNameCompilable()}");

                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }

    class TransactionPolicyViolationException : Exception
    {
        public TransactionPolicyViolationException(string message) : base(message + TransactionPolicyRationale) {}

        static readonly string TransactionPolicyRationale = $@"

Rationale: 
When accessing services on a bus it is very important to understand whether or not the service you are accessing will behave transactionally or not. 
If you make a mistake with regards to this you are likely to end up intermittent bugs resulting in corrupt data. 
Such bugs are notoriously hard both to debug and to reproduce at all.

In order to minimize your risk of encountering such problems we have runtime validations detecting if your usage of transactions in combination with services make sense.

Rules: 

* Within a transaction you are not allowed send any {typeof(BusApi.Remotable.NonTransactional.IMessage).FullName}
These messages will not respect the transaction so if you want to send them during a transaction you have to aknowledge that they will not by suppressing the current transaction before sending them.


* There must be a transaction for you to send a {typeof(BusApi.Remotable.ExactlyOnce.IMessage).FullName}
The whole point of these message types is to guarantee you exactly once delivery. Without a transaction while sending this guarantee is lost.

";
    }
}
