using System;
using Composable.DDD;

namespace Composable.Messaging.Commands
{
    public class Command : ValueObject<Command>, ICommand
    {
        public Guid Id { get; set; }

        protected Command()
            : this(Guid.NewGuid()) { }

        protected Command(Guid id) { Id = id; }
    }
}
