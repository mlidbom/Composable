using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System.Reflection;

namespace Composable.Messaging
{
    static class MessageInspector
    {
        static readonly HashSet<Type> SuccessfullyInspectedTypes = new HashSet<Type>();

        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect)
        {
            foreach(var type in eventTypesToInspect)
            {
                AssertValid(type);
            }
        }

        internal static void AssertValid<TMessage>() => AssertValid(typeof(TMessage));

        internal static void AssertValidToSend(IMessage message)
        {
            if(message is IRequiresTransactionalSendOperationMessage && Transaction.Current == null)
            {
                throw new Exception($"{message.GetType().FullName} is {nameof(IRequiresTransactionalSendOperationMessage)} but there is no transaction.");
            }

            if(message is IForbidTransactionalSendOperationMessage && Transaction.Current != null)
            {
                throw new Exception($"{message.GetType().FullName} is {nameof(IForbidTransactionalSendOperationMessage)} but there is a transaction.");
            }

            AssertValid(message.GetType());
        }

        internal static void AssertValid(Type type)
        {
            lock(SuccessfullyInspectedTypes)
            {
                if(SuccessfullyInspectedTypes.Contains(type)) return;

                if(type.Implements<ICommand>() && type.Implements<IEvent>())
                {
                    throw new Exception($"{type.FullName} implements both {typeof(ICommand)} and {typeof(IEvent)}.");
                }

                if(type.Implements<ICommand>() && type.Implements<IQuery>())
                {
                    throw new Exception($"{type.FullName} implements both {typeof(ICommand)} and {typeof(IQuery)}.");
                }

                if(type.Implements<IEvent>() && type.Implements<IQuery>())
                {
                    throw new Exception($"{type.FullName} implements both {typeof(IEvent)} and {typeof(IQuery)}.");
                }

                if(type.Implements<IRemoteMessage>() && type.Implements<ILocalMessage>())
                {
                    throw new Exception($"{type.FullName} implements both {typeof(IRemoteMessage)} and {typeof(ILocalMessage)}.");
                }

                if(type.Implements<IRequiresTransactionalSendOperationMessage>() && type.Implements<IForbidTransactionalSendOperationMessage>())
                {
                    throw new Exception($"{type.FullName} implements both {typeof(IRequiresTransactionalSendOperationMessage)} and {typeof(IForbidTransactionalSendOperationMessage)}.");
                }

                if(type.Implements<IQuery>() && !type.IsAbstract && !type.Implements(typeof(IQuery<>)))
                {
                    throw new Exception($"{type.FullName} implements only: {nameof(IQuery)}. Concrete types must implement {typeof(IQuery<>).GetFullNameCompilable()}");
                }

                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }
}
