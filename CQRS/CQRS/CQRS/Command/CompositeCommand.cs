using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Composable.CQRS.Command
{
    public abstract class CompositeCommand : Command
    {
        public abstract IEnumerable<SubCommand> GetContainedCommands();

        //TODO: remove. Needed until CVM is cleaned up 
        public abstract IEnumerable<Command> GetContainedCommandsOld();

        protected CompositeCommand() : this(Guid.NewGuid())
        {
        }

        protected CompositeCommand(Guid id) : base(id)
        {
        }
    }
}