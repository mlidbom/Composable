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

        internal static void AssertValidToSend(MessagingApi.IMessage message)
        {
            if(message is MessagingApi.Remote.ExactlyOnce.IRequireTransactionalSender && Transaction.Current == null)                              throw new Exception($"{message.GetType().FullName} is {nameof(MessagingApi.Remote.ExactlyOnce.IRequireTransactionalSender)} but there is no transaction.");
            if(message is MessagingApi.Remote.NonTransactional.IForbidTransactionalSend && Transaction.Current != null)                                 throw new Exception($"{message.GetType().FullName} is {nameof(MessagingApi.Remote.NonTransactional.IForbidTransactionalSend)} but there is a transaction.");

            AssertValid(message.GetType());
        }

        static void AssertValid(Type type)
        {
            lock(SuccessfullyInspectedTypes)
            {
                if(SuccessfullyInspectedTypes.Contains(type)) return;

                if(!type.Implements<MessagingApi.IMessage>())                                                                   throw new Exception($"{type.FullName} is not an {nameof(MessagingApi.IMessage)}");
                if(type.Implements<MessagingApi.ICommand>() && type.Implements<MessagingApi.IEvent>())                                       throw new Exception($"{type.FullName} implements both {typeof(MessagingApi.ICommand)} and {typeof(MessagingApi.IEvent)}.");
                if(type.Implements<MessagingApi.ICommand>() && type.Implements<MessagingApi.IQuery>())                                       throw new Exception($"{type.FullName} implements both {typeof(MessagingApi.ICommand)} and {typeof(MessagingApi.IQuery)}.");
                if(type.Implements<MessagingApi.IEvent>() && type.Implements<MessagingApi.IQuery>())                                         throw new Exception($"{type.FullName} implements both {typeof(MessagingApi.IEvent)} and {typeof(MessagingApi.IQuery)}.");
                if(type.Implements<MessagingApi.Remote.ISupportRemoteReceiver>() && type.Implements<MessagingApi.Local.IRequireLocalReceiver>())             throw new Exception($"{type.FullName} implements both {typeof(MessagingApi.Remote.ISupportRemoteReceiver)} and {typeof(MessagingApi.Local.IRequireLocalReceiver)}.");
                if(type.Implements<MessagingApi.Remote.ExactlyOnce.IRequireTransactionalSender>() && type.Implements<MessagingApi.Remote.NonTransactional.IForbidTransactionalSend>())  throw new Exception($"{type.FullName} implements both {typeof(MessagingApi.Remote.ExactlyOnce.IRequireTransactionalSender)} and {typeof(MessagingApi.Remote.NonTransactional.IForbidTransactionalSend)}.");
                if(type.Implements<MessagingApi.IQuery>() && !type.IsAbstract && !type.Implements(typeof(MessagingApi.IQuery<>)))            throw new Exception($"{type.FullName} implements only: {nameof(MessagingApi.IQuery)}. Concrete types must implement {typeof(MessagingApi.IQuery<>).GetFullNameCompilable()}");

                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }
}
