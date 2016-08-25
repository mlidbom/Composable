using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS;
using Composable.CQRS.Command;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using NUnit.Framework;
using Composable.System.Linq;

namespace CQRS.Tests.Command
{
    [TestFixture]
    public class CompositeCommandTests
    {
        private WindsorContainer _container;


        [SetUp]
        public void Setup()
        {
            _container = new WindsorContainer();
            _container.Register(
                Component.For<ICommandService>().ImplementedBy<CommandService>(),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<IWindsorContainer>().Instance(_container),
                Component.For<ICommandHandler<EditCommand>>().ImplementedBy<EditCommandHandler>()
                );

            EditCommandHandler.ExecuteCalled = false;
        }

        [Test]
        public void ShouldFindTheCommandsInCompositeCommand()
        {
            var somethingAndEditCommand = new SomethingAndEditCommand
                                          {
                                              EditCommand = new EditCommand(),
                                              SomethingCommand = new SomethingCommand()
                                          };

            var result = somethingAndEditCommand.GetContainedCommands().ToList();


            result.Should().HaveCount(2);
            result.Should().Contain(c => c.Name == "SomethingCommand" && c.Command is SomethingCommand);
        }

        [Test]
        public void CompositeCommandsThatFailsShouldBuildCorrectMemberPath()
        {
            var somethingAndEditCommand = new SomethingAndEditCommand
                                          {
                                              EditCommand = new EditCommand(),
                                              SomethingCommand = new SomethingCommand()
                                          };

            //Execute command
            var result = Assert.Throws<DomainCommandValidationException>(() => _container.Resolve<ICommandService>().Execute(somethingAndEditCommand));

            //CommandService should now rethrow a DomainCommandValidationException with correct member path
            result.Should().BeOfType<DomainCommandValidationException>();
            result.Message.Should().Be("EditedField is invalid");
            result.InvalidMembers.Should().HaveCount(1);
            result.InvalidMembers.First().Should().Be("EditCommand.EditedField");
        }

        [Test]
        public void CompositeCommandsThatFailsShouldBuildCorrectMemberPathWhenMultipleFieldAreInvalid()
        {
            var somethingAndEditCommand = new SomethingAndEditCommand
                                          {
                                              EditCommand = new EditCommand()
                                                            {
                                                                BothInvalid = true
                                                            },
                                              SomethingCommand = new SomethingCommand()
                                          };

            //Execute command
            var result = Assert.Throws<DomainCommandValidationException>(() => _container.Resolve<ICommandService>().Execute(somethingAndEditCommand));

            result.Message.Should().Be("EditedField and otherfield is invalid");
            result.InvalidMembers.Should().HaveCount(2);
            result.InvalidMembers.First().Should().Be("EditCommand.EditedField");
            result.InvalidMembers.Second().Should().Be("EditCommand.OtherEditedField");
        }

        public class SomethingAndEditCommand : CompositeCommand
        {
            public EditCommand EditCommand { private get; set; }
            public SomethingCommand SomethingCommand { private get; set; }

            override public IEnumerable<SubCommand> GetContainedCommands()
            {
                return new List<SubCommand>
                       {
                           new SubCommand(() => EditCommand),
                           new SubCommand(() => SomethingCommand)
                       };
            }
        }

        public class EditCommand : Composable.CQRS.Command.Command
        {
            public bool BothInvalid { get; set; }
            public string EditedField { get; set; }
            public string OtherEditedField { get; set; }
        }

        public class SomethingCommand : Composable.CQRS.Command.Command {}

        public class EditCommandHandler : ICommandHandler<EditCommand>
        {
            public static bool ExecuteCalled { get; set; }

            public void Execute(EditCommand command)
            {
                ExecuteCalled = true;
                if(command.BothInvalid)
                {
                    throw new DomainCommandValidationException("EditedField and otherfield is invalid",
                                                     () => command.EditedField,
                                                     () => command.OtherEditedField);
                }
                else
                {
                    throw new DomainCommandValidationException("EditedField is invalid", () => command.EditedField);
                }
            }
        }
    }
}
