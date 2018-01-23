using System;
using System.Collections.Generic;
using System.Transactions;
using Composable.System.Reflection;

namespace Composable.Messaging {
    static class MessageInspector
    {
        static readonly HashSet<Type> SuccessfullyInspectedTypes = new HashSet<Type>();

        internal static void AssertValid(IReadOnlyList<Type> eventTypesToInspect)
        {
            foreach(var type in eventTypesToInspect)
            {
                AssertTypeIsValid(type);
            }
        }

        public static void AssertValidToSend(IMessage message)
        {
            if(message is IRequiresTransactionalSendOperationMessage && Transaction.Current == null)
            {
                throw new Exception($"{message.GetType().FullName} is {nameof(IRequiresTransactionalSendOperationMessage)} but there is no transaction.");
            }

            if(message is IForbidTransactionalSendOperationMessage && Transaction.Current != null)
            {
                throw new Exception($"{message.GetType().FullName} is {nameof(IForbidTransactionalSendOperationMessage)} but there is a transaction.");
            }

            AssertTypeIsValid(message.GetType());
        }

        static void AssertTypeIsValid(Type type)
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
                SuccessfullyInspectedTypes.Add(type);
            }
        }
    }
}