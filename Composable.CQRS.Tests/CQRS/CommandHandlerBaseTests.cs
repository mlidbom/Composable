using System;
using Composable.CQRS;
using Composable.CQRS.Command;
using Composable.ServiceBus;
using Moq;
using NUnit.Framework;

namespace CQRS.Tests.CQRS
{
    [TestFixture]
    internal class CommandHandlerBaseTests
    {
        [Test]
        public void CommanHandlerBase_should_send_commandFailedEvent_and_rethrows_the_exception_thrown_by_the_inheritor()
        {
            //Arrange
            var busMock = new Mock<IServiceBus>(MockBehavior.Strict);
            busMock.Setup(bus => bus.Reply(It.IsAny<CommandExecutionExceptionResponse>()));

            var commandHandler = new CommandHandlerDummy(busMock.Object);
            var command = new CommandDummy();

            //Act
            Assert.Throws<NotImplementedException>(() => commandHandler.Handle(command));

            //Assert
            busMock.Verify(bus => bus.Reply(It.IsAny<CommandExecutionExceptionResponse>()), Times.Once());
        }

        public class CommandDummy : Composable.CQRS.Command.Command {}

        public class CommandHandlerDummy : CommandHandlerBase<CommandDummy>
        {
            public CommandHandlerDummy(IServiceBus bus) : base(bus, new MyCommandService()) {}
        }


        internal class MyCommandService : ICommandService
        {
            public CommandResult Execute<TCommand>(TCommand command)
            {
                throw new NotImplementedException();
            }
        }
    }
}
