using System;
using Composable.DDD;

namespace Composable.Messaging.Commands
{
    public class Command : ValueObject<Command>, ICommand
    {
        public Guid MessageId { get; private set; }

        protected Command()
            : this(Guid.NewGuid()) {}

        Command(Guid id) => MessageId = id;
    }

    public class Command<TResult> : Command, ICommand<TResult> where TResult : IMessage {}
}
