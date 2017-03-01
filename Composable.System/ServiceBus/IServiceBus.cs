using System;
using Composable.CQRS.Command;

namespace Composable.ServiceBus
{
  public interface IServiceBus : IInProcessServiceBus
    {
        void SendAtTime(DateTime sendAt, ICommand message);
    }
}
