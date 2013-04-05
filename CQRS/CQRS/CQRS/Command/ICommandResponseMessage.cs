using System;
using NServiceBus;

namespace Composable.CQRS.Command
{
    public interface ICommandResponseMessage : IMessage
    {
        Guid CommandId { get; }
    }
}