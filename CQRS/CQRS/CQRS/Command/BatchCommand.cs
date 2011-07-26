using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Composable.CQRS.Command
{
    public class BatchCommand : CompositeCommand
    {
        private ReadOnlyCollection<Command> _commands;

        public BatchCommand(IEnumerable<Command> commands) {
            _commands = new List<Command>(commands).AsReadOnly();
        }

        public override IEnumerable<Command> GetContainedCommands()
        {
            return _commands;
        }
    }
}
