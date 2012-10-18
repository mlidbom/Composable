using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Composable.CQRS.Command
{
    //TODO: remove. Needed until CVM is cleaned up 
    public class BatchCommand : CompositeCommand
    {
        private ReadOnlyCollection<Command> _commands;

        public BatchCommand(IEnumerable<Command> commands) {
            _commands = new List<Command>(commands).AsReadOnly();
        }

        override public IEnumerable<SubCommand> GetContainedCommands()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Command> GetContainedCommandsOld()
        {
            return _commands;
        }
    }
}
