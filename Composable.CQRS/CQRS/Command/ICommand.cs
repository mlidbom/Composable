using System;

namespace Composable.CQRS.Command
{
    public interface ICommand : NServiceBus.ICommand
    {
        Guid Id { get; set; }
    }
}
