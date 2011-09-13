using System;
using System.Collections.Generic;
using Composable.DDD;

namespace Composable.CQRS.Command
{
    public abstract class CompositeCommand : Command
    {
        public abstract IEnumerable<Command> GetContainedCommands();

        protected CompositeCommand() : this(Guid.NewGuid())
        {
        }

        protected CompositeCommand(Guid id) : base(id)
        {
        }
    }
}