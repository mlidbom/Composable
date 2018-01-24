using System;
using Composable.DDD;

namespace Composable.Messaging.Commands
{
    public class ExactlyOnceCommand : ValueObject<ExactlyOnceCommand>, IExactlyOnceCommand
    {
        public Guid MessageId { get; private set; }

        protected ExactlyOnceCommand()
            : this(Guid.NewGuid()) {}

        ExactlyOnceCommand(Guid id) => MessageId = id;
    }

    public class ExactlyOnceCommand<TResult> : ExactlyOnceCommand, IExactlyOnceCommand<TResult> {}
}
