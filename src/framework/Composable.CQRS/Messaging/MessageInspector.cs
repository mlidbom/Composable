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
            if(message is IRequireTransactionalSender && Transaction.Current == null)                              throw new Exception($"{message.GetType().FullName} is {nameof(IRequireTransactionalSender)} but there is no transaction.");
            if(message is IForbidTransactionalSend && Transaction.Current != null)                               throw new Exception($"{message.GetType().FullName} is {nameof(IForbidTransactionalSend)} but there is a transaction.");

            AssertValid(message.GetType());
        }

        static void AssertValid(Type type)
        {
            lock(SuccessfullyInspectedTypes)
            {
                if(SuccessfullyInspectedTypes.Contains(type)) return;

                if(!type.Implements<IMessage>())                                                                 throw new Exception($"{type.FullName} is not an {nameof(IMessage)}");
                if(type.Implements<ICommand>() && type.Implements<IEvent>())                                     throw new Exception($"{type.FullName} implements both {typeof(ICommand)} and {typeof(IEvent)}.");
                if(type.Implements<ICommand>() && type.Implements<IQuery>())                                     throw new Exception($"{type.FullName} implements both {typeof(ICommand)} and {typeof(IQuery)}.");
                if(type.Implements<IEvent>() && type.Implements<IQuery>())                                       throw new Exception($"{type.FullName} implements both {typeof(IEvent)} and {typeof(IQuery)}.");
                if(type.Implements<ISupportRemoteReceiver>() && type.Implements<IOnlyLocalReceiver>())           throw new Exception($"{type.FullName} implements both {typeof(ISupportRemoteReceiver)} and {typeof(IOnlyLocalReceiver)}.");
                if(type.Implements<IRequireTransactionalSender>() && type.Implements<IForbidTransactionalSend>())  throw new Exception($"{type.FullName} implements both {typeof(IRequireTransactionalSender)} and {typeof(IForbidTransactionalSend)}.");
                if(type.Implements<IQuery>() && !type.IsAbstract && !type.Implements(typeof(IQuery<>)))          throw new Exception($"{type.FullName} implements only: {nameof(IQuery)}. Concrete types must implement {typeof(IQuery<>).GetFullNameCompilable()}");

                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }
}
