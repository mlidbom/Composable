using System;
using System.Linq.Expressions;
using Composable.System.Linq;

namespace Composable.CQRS.Command
{
    public class SubCommand : ISubCommand
    {
        private Func<Command> _accessor;
        
        public string Name { get; private set; }
        
        public Command Command { get { return _accessor();  } }

        public SubCommand(Expression<Func<Command>> commandToFind)
        {
            Name = ExpressionUtil.ExtractMemberName(commandToFind);
            _accessor = commandToFind.Compile();
        }
    }
}