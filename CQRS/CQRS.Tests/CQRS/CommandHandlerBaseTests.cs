using System;
using Composable.CQRS.Command;
using Composable.ServiceBus;
using Moq;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace CQRS.Tests.CQRS
{
    [TestFixture]
    class CommandHandlerBaseTests
    {
        [Test]
        public void CommanHandlerBase_should_send_commandFailedEvent_and_rethrows_the_exception_thrown_by_the_inheritor()
        {
            //Arrange
            var busMock = new Mock<IServiceBus>(MockBehavior.Strict);
            busMock.Setup(bus => bus.Reply(It.IsAny<CommandFailed>()));

            var commandHandler = new CommandHandlerDummy(busMock.Object);
            var command = new CommandDummy();

            //Act
            Assert.Throws<NotImplementedException>(() => commandHandler.Handle(command));

            //Assert
            busMock.Verify(bus => bus.Reply(It.IsAny<CommandFailed>()), Times.Once());    
        }

        public class CommandDummy : Composable.CQRS.Command.Command
        { }
        public class CommandHandlerDummy : CommandHandlerBase<CommandDummy, CommandFailedDummy>
        {
            public CommandHandlerDummy(IServiceBus bus) : base(bus) { }

            override protected void HandleCommand(CommandDummy command)
            {
                throw new NotImplementedException();
            }
        }
        public class CommandFailedDummy:CommandFailed
        {
        }
        
    }
}
