using System.Collections.Generic;
using Castle.Windsor;
using Composable.ServiceBus;
using Microsoft.Practices.ServiceLocation;
using NServiceBus;
using System.Linq;
using Composable.System.Reflection;

namespace Composable.CQRS.Testing
{
    public class DummyServiceBus : IServiceBus
    {
        private readonly IServiceLocator _serviceLocator;

        public DummyServiceBus(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        private readonly IList<object> _published = new List<object>();

        public IEnumerable<object> Published { get { return _published; } }

        public void Reset()
        {
            _published.Clear();
        }

        public void Publish(object message)
        {
            ((dynamic)this).Publish((dynamic)message);
        }

        public void Publish<TMessage>(TMessage message) where TMessage : IMessage
        {
            var handlerTypes = message.GetType().GetAllTypesInheritedOrImplemented()                                
                .Where(t => t.Implements(typeof(IMessage)))
                .Select(t => typeof(IHandleMessages<>).MakeGenericType(t))
                .ToArray();

             _published.Add(message);

            var handlers = new List<object>();
            foreach(var handlerType in handlerTypes)
            {
                handlers.AddRange(new List<object>( _serviceLocator.GetAllInstances(handlerType)));
            }

            //var handlers = handlerTypes.SelectMany(type =>_serviceLocator.GetAllInstances(type)).ToArray();
            foreach(dynamic handler in handlers)
            {
                handler.Handle(message);
            }
        }
    }
}