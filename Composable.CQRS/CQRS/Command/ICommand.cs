using System;
using Composable.ServiceBus;

namespace Composable.CQRS.Command
{
    public interface ICommand : IMessage
    {
        Guid Id { get; }
    }
}
