using System;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public interface IServiceBus : IInProcessServiceBus
    {
        void SendAtTime(DateTime sendAt, ICommand message);
    }
}
