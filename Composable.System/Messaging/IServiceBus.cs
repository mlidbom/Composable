using System;

namespace Composable.Messaging
{
  using Composable.Messaging.Bus;
  using Composable.Messaging.Commands;

  public interface IServiceBus : IInProcessServiceBus
    {
        void SendAtTime(DateTime sendAt, ICommand message);
    }
}
