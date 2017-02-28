using System;

namespace Composable.ServiceBus
{
    public interface IServiceBus
    {
        void Publish(object message);
        void Send(object message);
        void Reply(object message);
        void SendAtTime(DateTime sendAt, object message);
    }
}
