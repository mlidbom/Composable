using System;
using Composable.ServiceBus;

namespace Composable.CQRS.Command
{
    public interface ICommand : IMessage
    {
        //Review:mlidbo: Should this have a setter!?
        Guid Id { get; set; }
    }
}
