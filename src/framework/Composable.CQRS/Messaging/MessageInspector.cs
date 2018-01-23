using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable.Messaging
{
    static class MessageInspector
    {
        static readonly HashSet<Type> SuccessfullyInspectedTypes = new HashSet<Type>();

        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect) => eventTypesToInspect.ForEach(AssertValid);

        internal static void AssertValid<TMessage>() => AssertValid(typeof(TMessage));

        internal static void AssertValidToSend(IMessage message)
        {
            if(message is IRequireTransactionalSendOperation && Transaction.Current == null) throw new Exception($"{message.GetType().FullName} is {nameof(IRequireTransactionalSendOperation)} but there is no transaction.");
            if(message is IForbidTransactionalSendOperation && Transaction.Current != null)  throw new Exception($"{message.GetType().FullName} is {nameof(IForbidTransactionalSendOperation)} but there is a transaction.");

            AssertValid(message.GetType());
        }

        static void AssertValid(Type type)
        {
            lock(SuccessfullyInspectedTypes)
            {
                if(SuccessfullyInspectedTypes.Contains(type)) return;

                if(!type.Implements<IMessage>())                                                                                  throw new Exception($"{type.FullName} is not an {nameof(IMessage)}");
                if(type.Implements<ICommand>() && type.Implements<IEvent>())                                                      throw new Exception($"{type.FullName} implements both {typeof(ICommand)} and {typeof(IEvent)}.");
                if(type.Implements<ICommand>() && type.Implements<IQuery>())                                                      throw new Exception($"{type.FullName} implements both {typeof(ICommand)} and {typeof(IQuery)}.");
                if(type.Implements<IEvent>() && type.Implements<IQuery>())                                                        throw new Exception($"{type.FullName} implements both {typeof(IEvent)} and {typeof(IQuery)}.");
                if(type.Implements<ISupportRemoteDelivery>() && type.Implements<IOnlyLocalDelivery>())                            throw new Exception($"{type.FullName} implements both {typeof(ISupportRemoteDelivery)} and {typeof(IOnlyLocalDelivery)}.");
                if(type.Implements<IRequireTransactionalSendOperation>() && type.Implements<IForbidTransactionalSendOperation>()) throw new Exception($"{type.FullName} implements both {typeof(IRequireTransactionalSendOperation)} and {typeof(IForbidTransactionalSendOperation)}.");
                if(type.Implements<IQuery>() && !type.IsAbstract && !type.Implements(typeof(IQuery<>)))                           throw new Exception($"{type.FullName} implements only: {nameof(IQuery)}. Concrete types must implement {typeof(IQuery<>).GetFullNameCompilable()}");

                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }
}
