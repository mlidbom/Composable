using System;
using Composable.CQRS.Command;
using Composable.ServiceBus;
using FluentAssertions;
using JetBrains.Annotations;
using Moq;
using NServiceBus.Saga;
using NUnit.Framework;

namespace CQRS.Tests.CQRS
{
    [TestFixture]
    class SagaCommandHandlerBaseTests
    {
        [Test]
        [ExpectedException(typeof(NotImplementedException))]
        public void Saga_should_send_commandFailedEvent_and_throw_underlying_Exceptions()
        {
            //Arrange
            var busMock = new Mock<IServiceBus>(MockBehavior.Strict);
            busMock.Setup(bus => bus.Publish(It.IsAny<CommandDomainValidationExceptionResponse>()));

            var saga = new SagaDummy(busMock.Object);
            
            var command = new CommandDummy();

            //Act
            try
            {
                saga.Handle(command);
                
            }
            finally
            {
                //Assert
                busMock.Verify(bus => bus.Publish(It.IsAny<CommandDomainValidationExceptionResponse>()), Times.Once());
                saga.Completed.Should().Be(true);
            }
        }

        private class CommandDummy : Composable.CQRS.Command.Command
        { }

        [UsedImplicitly]
        public class SagaDataDummy:ISagaEntity
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
        }

        private class SagaDummy : SagaBase<SagaDataDummy, CommandDummy, CommandDomainValidationExceptionResponse> 
        {
            public SagaDummy(IServiceBus bus) : base(bus) { }

            override protected void HandleBaseStartCommand(CommandDummy command)
            {
                throw new NotImplementedException();
            }
        }
        
    }
}