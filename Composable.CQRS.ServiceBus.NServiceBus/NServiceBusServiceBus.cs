using System;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.ServiceBus;
using NServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class NServiceBusServiceBus : INservicebusServicebus
    {
        private IBus _bus;
        private IMessageInterceptor _interceptor;
        
        private void AddEnvironmentNameHeader()
        {
            _bus.OutgoingHeaders[EndpointCfg.EnvironmentNameMessageHeaderName] = EndpointCfg.EnvironmentName;
        }


        public NServiceBusServiceBus(IBus bus, IMessageInterceptor interceptor = null)
        {
            _interceptor = interceptor ?? EmptyMessageInterceptor.Instance;
            _bus = bus;
        }

        public virtual void Publish(Object message)
        {
            _interceptor.BeforePublish(message);
            AddEnvironmentNameHeader();
            _bus.Publish((IMessage)message);
        }

        public virtual void SendLocal(object message)
        {
            _interceptor.BeforeSendLocal(message);
            AddEnvironmentNameHeader();
            _bus.SendLocal((IMessage)message);
        }

        public virtual void Send(object message)
        {
            _interceptor.BeforeSend(message);
            AddEnvironmentNameHeader();
            _bus.Send((IMessage) message);
        }

        public void Reply(object message)
        {
            _interceptor.BeforeSend(message);
            AddEnvironmentNameHeader();
            _bus.Reply((IMessage)message);
        }

        public void SendAtTime(DateTime sendAt, object message) { _bus.Defer(sendAt, message); }
    }
}