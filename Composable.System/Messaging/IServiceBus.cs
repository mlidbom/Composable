using System;

namespace Composable.Messaging
{
  using Composable.Messaging.Buses;
  using Composable.Messaging.Commands;

  public interface IServiceBus : IInProcessServiceBus
    {
        void SendAtTime(DateTime sendAt, ICommand message);
    }
}
