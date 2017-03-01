using System;
using Composable.CQRS.Command;
using Composable.CQRS.EventSourcing;

namespace Composable.ServiceBus
{
    public interface IInProcessServiceBus
    {
        void Publish(IEvent anEvent);
        void Send(ICommand message);
    }

    public interface IServiceBus : IInProcessServiceBus
    {
        void SendAtTime(DateTime sendAt, ICommand message);
    }
}
