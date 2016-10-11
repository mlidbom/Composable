using System;
using Composable.ServiceBus;

namespace Composable.CQRS.Command
{
    public interface ICommandResponseMessage : IMessage
    {
        Guid CommandId { get; }
    }
}