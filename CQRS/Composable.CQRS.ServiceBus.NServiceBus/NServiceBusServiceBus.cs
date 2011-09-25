using System;
using NServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public class NServiceBusServiceBus : INservicebusServicebus
    {
        private IBus _bus;
        private IMessageInterceptor _interceptor;

        public NServiceBusServiceBus(IBus bus, IMessageInterceptor interceptor = null)
        {
            _interceptor = interceptor ?? EmptyMessageInterceptor.Instance;
            _bus = bus;
        }

        public virtual void Publish(Object message)
        {
            _interceptor.BeforePublish(message);
            _bus.Publish((IMessage)message);
        }

        public virtual void SendLocal(object message)
        {
            _interceptor.BeforeSendLocal(message);
            _bus.SendLocal((IMessage)message);
        }

        public virtual void Send(object message)
        {
            _interceptor.BeforeSend(message);
            _bus.Send((IMessage) message);
        }
    }
}