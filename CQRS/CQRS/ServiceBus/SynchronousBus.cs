using System.Collections.Generic;
using System.Linq;
using Castle.Windsor;
using Composable.System.Reflection;
using Composable.System.Transactions;
using NServiceBus;

namespace Composable.ServiceBus
{
    public class SynchronousBus : IServiceBus
        {
            private readonly IWindsorContainer _serviceLocator;

            public SynchronousBus(IWindsorContainer serviceLocator)
            {
                _serviceLocator = serviceLocator;
            }

            public void Publish(object message)
            {
                ((dynamic)this).Publish((dynamic)message);
            }

            private void Publish<TMessage>(TMessage message) where TMessage : IMessage
            {
                var handlerTypes = message.GetType().GetAllTypesInheritedOrImplemented()
                    .Where(t => t.Implements(typeof(IMessage)))
                    .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
                    .ToArray();


                var handlers = new List<object>();
                foreach (var handlerType in handlerTypes)
                {
                    handlers.AddRange(_serviceLocator.ResolveAll(handlerType).Cast<object>());
                }

                InTransaction.Execute(() =>
                {
                    foreach (dynamic handler in handlers)
                    {
                        handler.Handle((dynamic)message);
                    }
                });
            }

            public void SendLocal(object message)
            {
                Publish(message);
            }
       
        } 
    }
