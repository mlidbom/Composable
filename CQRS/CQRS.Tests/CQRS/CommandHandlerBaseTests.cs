using System;
using Composable.CQRS.Command;
using Composable.ServiceBus;
using Moq;
using NUnit.Framework;

namespace CQRS.Tests.CQRS
{
    [TestFixture]
    class CommandHandlerBaseTests
    {
        [Test]
        [ExpectedException (typeof(NotImplementedException))]
        public void CommanHandlerBase_should_send_commandFailedEvent_and_throw_underlying_Exceptions()
        {
            //Arrange
            var busMock = new Mock<IServiceBus>(MockBehavior.Strict);
            busMock.Setup(bus => bus.Publish(It.IsAny<CommandFailed>()));

            var commandHandler = new CommandHandlerDummy(busMock.Object);
            var command = new CommandDummy();

            //Act
            try
            {
                commandHandler.Handle(command);
            }
            finally
            {
                //Assert
                busMock.Verify(bus => bus.Publish(It.IsAny<CommandFailed>()), Times.Once());    
            }
        }

        public class CommandDummy : Composable.CQRS.Command.Command
        { }
        public class CommandHandlerDummy : CommandHandlerBase<CommandDummy>
        {
            public CommandHandlerDummy(IServiceBus bus) : base(bus) { }

            override protected void HandleCommand(CommandDummy command)
            {
                throw new NotImplementedException();
            }
        }
    }
}
