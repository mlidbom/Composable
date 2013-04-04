using System;
using Composable.DDD;

namespace Composable.CQRS.Command
{
    public class Command : ValueObject<Command>, ICommand
    {
        public Guid Id { get; set; }

        protected Command() : this(Guid.NewGuid()) {}

        protected Command(Guid id)
        {
            Id = id;
        }
    }
}
