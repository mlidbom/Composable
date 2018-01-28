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

        internal static void AssertValidToSend(BusApi.IMessage message)
        {
            if(message is BusApi.Remote.ExactlyOnce.IRequireTransactionalSender && Transaction.Current == null)                              throw new Exception($"{message.GetType().FullName} is {nameof(BusApi.Remote.ExactlyOnce.IRequireTransactionalSender)} but there is no transaction.");
            if(message is BusApi.Remote.NonTransactional.IForbidTransactionalSend && Transaction.Current != null)                                 throw new Exception($"{message.GetType().FullName} is {nameof(BusApi.Remote.NonTransactional.IForbidTransactionalSend)} but there is a transaction.");

            AssertValid(message.GetType());
        }

        static void AssertValid(Type type)
        {
            lock(SuccessfullyInspectedTypes)
            {
                if(SuccessfullyInspectedTypes.Contains(type)) return;

                if(!type.Implements<BusApi.IMessage>())                                                                   throw new Exception($"{type.FullName} is not an {nameof(BusApi.IMessage)}");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IEvent>())                                       throw new Exception($"{type.FullName} implements both {typeof(BusApi.ICommand)} and {typeof(BusApi.IEvent)}.");
                if(type.Implements<BusApi.ICommand>() && type.Implements<BusApi.IQuery>())                                       throw new Exception($"{type.FullName} implements both {typeof(BusApi.ICommand)} and {typeof(BusApi.IQuery)}.");
                if(type.Implements<BusApi.IEvent>() && type.Implements<BusApi.IQuery>())                                         throw new Exception($"{type.FullName} implements both {typeof(BusApi.IEvent)} and {typeof(BusApi.IQuery)}.");
                if(type.Implements<BusApi.Remote.ISupportRemoteReceiver>() && type.Implements<BusApi.Local.IRequireLocalReceiver>())             throw new Exception($"{type.FullName} implements both {typeof(BusApi.Remote.ISupportRemoteReceiver)} and {typeof(BusApi.Local.IRequireLocalReceiver)}.");
                if(type.Implements<BusApi.Remote.ExactlyOnce.IRequireTransactionalSender>() && type.Implements<BusApi.Remote.NonTransactional.IForbidTransactionalSend>())  throw new Exception($"{type.FullName} implements both {typeof(BusApi.Remote.ExactlyOnce.IRequireTransactionalSender)} and {typeof(BusApi.Remote.NonTransactional.IForbidTransactionalSend)}.");
                if(type.Implements<BusApi.IQuery>() && !type.IsAbstract && !type.Implements(typeof(BusApi.IQuery<>)))            throw new Exception($"{type.FullName} implements only: {nameof(BusApi.IQuery)}. Concrete types must implement {typeof(BusApi.IQuery<>).GetFullNameCompilable()}");

                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }
}
