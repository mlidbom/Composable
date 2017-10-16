using System;
using Composable.DDD;

namespace Composable.Messaging.Commands
{
    public class DomainCommand : ValueObject<DomainCommand>, IDomainCommand
    {
        public Guid MessageId { get; private set; }

        protected DomainCommand()
            : this(Guid.NewGuid()) {}

        DomainCommand(Guid id) => MessageId = id;
    }

    public class DomainCommand<TResult> : DomainCommand, IDomainCommand<TResult> {}
}
