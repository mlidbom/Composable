using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Messaging
{
    static partial class MessageInspector
    {
        static readonly object Lock = new object();
        static HashSet<Type> _successfullyInspectedTypes = new HashSet<Type>();

        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(AssertValid);

        internal static void AssertValid<TMessage>() => AssertValid(typeof(TMessage));

        internal static void AssertValidToSendRemote(BusApi.IMessage message)
        {
            if(message is BusApi.StrictlyLocal.IMessage strictlyLocalMessage) throw new AttemptToSendStrictlyLocalMessageRemotely(strictlyLocalMessage);
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IRequireTransactionalSender).FullName} but there is no transaction.");
            if(message is BusApi.IForbidTransactionalRemoteSender && Transaction.Current != null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IForbidTransactionalRemoteSender).FullName} but there is a transaction.");
            if(message is BusApi.Remotable.IAtMostOnceMessage atMostOnce && atMostOnce.DeduplicationId == Guid.Empty) throw new Exception($"{nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)} was Guid.Empty for message of type: {message.GetType().FullName}");

            AssertValid(message.GetType());
        }

        internal static void AssertValidToSendLocal(BusApi.IMessage message)
        {
            if(message is BusApi.IRequireTransactionalSender && Transaction.Current == null) throw new TransactionPolicyViolationException($"{message.GetType().FullName} is {typeof(BusApi.IRequireTransactionalSender).FullName} but there is no transaction.");

            AssertValid(message.GetType());
        }

        static void AssertValid(Type type)
        {
            if(_successfullyInspectedTypes.Contains(type)) return;

            lock(Lock)
            {
                if(_successfullyInspectedTypes.Contains(type)) return;

                if(!type.Implements<BusApi.IMessage>()) throw new Exception($"{type.FullName} is not an {nameof(BusApi.IMessage)}");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IEvent>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.ICommand).FullName} and {typeof(BusApi.IEvent).FullName}.");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IQuery>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.ICommand).FullName} and {typeof(BusApi.IQuery).FullName}.");
                if(type.Implements<BusApi.IEvent>() && type.Implements<BusApi.IQuery>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.IEvent).FullName} and {typeof(BusApi.IQuery).FullName}.");
                if(type.Implements<BusApi.Remotable.IMessage>() && type.Implements<BusApi.StrictlyLocal.IMessage>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.Remotable.IMessage).FullName} and {typeof(BusApi.StrictlyLocal.IMessage).FullName}.");
                if(type.Implements<BusApi.IRequireTransactionalSender>() && type.Implements<BusApi.IForbidTransactionalRemoteSender>()) throw new MessageTypeDesignViolationException($"{type.FullName} implements both {typeof(BusApi.IRequireTransactionalSender).FullName} and {typeof(BusApi.IForbidTransactionalRemoteSender).FullName}.");
                if(type.Implements<BusApi.IQuery>() && !type.IsAbstract && !type.Implements(typeof(BusApi.IQuery<>))) throw new MessageTypeDesignViolationException($"{type.FullName} implements only: {typeof(BusApi.IQuery).FullName}. Concrete types must implement {typeof(BusApi.IQuery<>).GetFullNameCompilable()}");

                if(type.Implements<BusApi.Remotable.AtMostOnce.ICommand>())
                {
                    var instance = (BusApi.Remotable.AtMostOnce.ICommand)Constructor.CreateInstance(type);
                    if(instance.DeduplicationId != Guid.Empty)
                    {
                        throw new MessageTypeDesignViolationException($@"The default constructor of {type.GetFullNameCompilable()} sets {nameof(BusApi.Remotable.IAtMostOnceMessage)}.{nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)} to a value other than Guid.Empty.
Since {type.GetFullNameCompilable()} is an {typeof(BusApi.Remotable.AtMostOnce.ICommand).GetFullNameCompilable()} this is very likely to break the exactly once guarantee.
For instance: If you bind this command in a web UI and forget to bind the {nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)} then the infrastructure will be unable to realize that this is NOT the correct originally created {nameof(BusApi.Remotable.IAtMostOnceMessage.DeduplicationId)}.
This in turn means that if your user clicks multiple times the command may well be both sent and handled multiple times. Thus breaking the exactly once guarantee. The same thing if a Single Page Application receives an HTTP timeout and retries the command. 
And another example: If you make the setter private many serialization technologies will not be able to maintain the value of the property. But since you used this constructor the property will have a value. A new one each time the instance is deserialized. Again breaking the at most once guarantee.
");
                    }
                }

                _successfullyInspectedTypes = new HashSet<Type>(_successfullyInspectedTypes) {type};
            }
        }
    }
}
