using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary>
    /// Sends/Publishes messages to <see cref="IHandleMessages{T}"/> implementations registered in the <see cref="IWindsorContainer"/>.
    /// </summary>
    [UsedImplicitly]
    public class SynchronousBus : IServiceBus
    {
        protected readonly IWindsorContainer Container;
        private readonly List<IInvokeMessages> _invokers;

        public SynchronousBus(IWindsorContainer container)
        {
            Container = container;
            _invokers = new List<IInvokeMessages> { new InProcessMessageHandlerInvoker(container), new DefaultMessageHandlerInvoker(container) };
        }

        public virtual void Publish(object message)
        {
            PublishLocal(message);
        }

        public virtual bool Handles(object message)
        {
            return _invokers.Any(invoker => invoker.Invokes(message));
        }

        protected virtual void PublishLocal(object message)
        {
            using(Container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using(var transactionalScope = Container.BeginTransactionalUnitOfWorkScope())
                {
                    foreach (var messageHandlerInvoker in _invokers.Where(invoker => invoker.Invokes(message)))
                    {
                        messageHandlerInvoker.Invoke(message);
                    }
                    
                    transactionalScope.Commit();
                }
            }
        }

        protected virtual void SyncSendLocal(object message)
        {
            using(Container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using(var transactionalScope = Container.BeginTransactionalUnitOfWorkScope())
                {
                    if (_invokers.None(invoker => invoker.Invokes(message)))
                    {
                        throw new NoHandlerException(message.GetType());
                    }

                    //todo - needs to resolve handers here as well as in invoker.... :-(
                    var list = new List<object>();
                    _invokers.ForEach(invoker => list.AddRange(invoker.ResolveMessageHandlers(message)));
                    AssertOnlyOneHandlerRegistered(message, list);

                    foreach (var messageHandlerInvoker in _invokers.Where(invoker => invoker.Invokes(message)))
                    {
                        messageHandlerInvoker.Invoke(message);
                    }
                    
                    transactionalScope.Commit();
                }
            }
        }

        public virtual void SendLocal(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Send(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Reply(object message)
        {
            SyncSendLocal(message);
        }

        private static void AssertOnlyOneHandlerRegistered(object message, List<object> handlers)
        {
            var realHandlers = handlers.Except(handlers.OfType<ISynchronousBusMessageSpy>()).ToList();
            if (realHandlers.Count() > 1)
            {
                throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
            }
        }
    }

    public class NoHandlerException : Exception
    {
        public NoHandlerException(Type messageType):base("No handler registered for message type: {0}".FormatWith(messageType.FullName))
        {
            
        }
    }

    public class MultipleMessageHandlersRegisteredException : Exception
    {
        public MultipleMessageHandlersRegisteredException(object message, List<object> handlers):base(CreateMessage(message,handlers))
        {
        }

        private static string CreateMessage(object message, List<object> handlers)
        {
            var exceptionMessage = "There are multiple handlers registered for the message type:{0}.\nIf you are getting this because you have registered a listener as a test spy have your listener implement ISynchronousBusMessageSpy and the exception will disappear"
                .FormatWith(message.GetType());
            handlers.Select(handler => handler.GetType())
                .ForEach(handlerType => exceptionMessage += "{0}{1}".FormatWith(Environment.NewLine, handlerType.FullName)                
                );
            return exceptionMessage;

        }
    }

    //public class PublishToAllSubscribersSubscriberFilter : ISynchronousBusSubscriberFilter
    //{
    //    public static readonly ISynchronousBusSubscriberFilter Instance = new PublishToAllSubscribersSubscriberFilter();

    //    public bool PublishMessageToHandler(object message, object handler)
    //    {
    //        return true;
    //    }
    //}
}
