using System;

namespace Composable.Messaging
{
    public interface ICommand : IMessage
    {
        Guid Id { get; }
    }
}
