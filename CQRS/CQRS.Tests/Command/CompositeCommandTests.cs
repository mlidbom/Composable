using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.Command;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.Command
{
    [TestFixture]
    public class CompositeCommandTests
    {

        [Test]
        public void ShouldFindTheCommandsInCompositeCommand()
        {
            var compositeCommand = new EditThisAndEditSomethingElse();
            var subCommand = new SubCommand(() => compositeCommand);
            var result = compositeCommand.GetContainedCommands().ToList();
            result.Should().HaveCount(2);
            result.Should().Contain(c => c.Name == "EditSomething" && c.Command is EditSomethingCommand);
        }

        public class EditThisAndEditSomethingElse : CompositeCommand
        {
            public EditSomethingCommand EditSomething { get; private set; }
            public EditSomethingElseCommand EditSomethingElse { get; private set; }

            public EditThisAndEditSomethingElse()
            {
                EditSomething = new EditSomethingCommand();
                EditSomethingElse = new EditSomethingElseCommand();
            }

            override public IEnumerable<SubCommand> GetContainedCommands()
            {
                return new List<SubCommand>()
                   {
                       new SubCommand(() => EditSomething),
                       new SubCommand(() => EditSomethingElse)
                   };
            }

            //TODO: remove. Needed until CVM is cleaned up 
            override public IEnumerable<Composable.CQRS.Command.Command> GetContainedCommandsOld()
            {
                throw new System.NotImplementedException();
            }
        }

        public class EditSomethingElseCommand : Composable.CQRS.Command.Command { }

        public class EditSomethingCommand : Composable.CQRS.Command.Command
        {

        }
    }
}
