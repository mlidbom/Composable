using System;
using Composable.CQRS.Command;
using Composable.ServiceBus;
using FluentAssertions;
using Machine.Specifications;
using Moq;
using NUnit.Framework;

namespace Composable.CQRS.Specs.CQRS
{
    [Subject("")]
    public class CommandHandlerBaseSpecification_
    {
        class when_executing_command_handler
        {
            static Mock<ICommandService> commandServiceMock = null;
            static Mock<IServiceBus> busMock = null;
            static MyDelegatingToCommandServiceCommandHandler commandHandler = null;
            Establish before = () =>
                               {
                                   commandServiceMock = new Mock<ICommandService>(MockBehavior.Strict);
                                   busMock = new Mock<IServiceBus>(MockBehavior.Strict);
                                   commandHandler = new MyDelegatingToCommandServiceCommandHandler(busMock.Object, commandServiceMock.Object);
                               };

            class when_commandService_throws_NotImplementedException
            {
                static NotImplementedException exception = null;
                Establish before = () =>
                                   {
                                       commandServiceMock
                                           .Setup(service => service.Execute<ICommand>(Moq.It.IsAny<ThrowNotImplementedExceptionCommand>()))
                                           .Throws<NotImplementedException>();

                                       busMock.Setup(bus => bus.Reply(Moq.It.IsAny<CommandExecutionExceptionResponse>()));

                                       exception =
                                           Assert.Throws<NotImplementedException>(
                                               () => commandHandler.Handle(new ThrowNotImplementedExceptionCommand()));
                                   };

                Machine.Specifications.It NotImplementedException_is_thrown_by_command_handler = () => exception.Should().NotBeNull();
                Machine.Specifications.It reply_with_CommandExecutionExceptionResponse_is_sent_on_the_bus =
                    () => busMock.Verify(bus => bus.Reply(Moq.It.IsAny<CommandExecutionExceptionResponse>()));
            }

            class when_commandService_throws_DomainCommandValidationException
            {
                static DomainCommandValidationException exception = null;
                Establish before = () =>
                                   {
                                       commandServiceMock
                                           .Setup(service => service.Execute<ICommand>(Moq.It.IsAny<ThrowDomainCommandValidationExceptionCommand>()))
                                           .Callback(() => { throw new DomainCommandValidationException("AMessage"); });

                                       busMock.Setup(bus => bus.Reply(Moq.It.IsAny<CommandDomainValidationExceptionResponse>()));

                                       exception =
                                           Assert.Throws<DomainCommandValidationException>(
                                               () => commandHandler.Handle(new ThrowDomainCommandValidationExceptionCommand()));
                                   };

                Machine.Specifications.It DomainCommandValidationException_is_thrown_by_command_handler = () => exception.Should().NotBeNull();
                Machine.Specifications.It reply_with_CommandDomainValidationExceptionResponse_is_sent_on_the_bus =
                    () => busMock.Verify(bus => bus.Reply(Moq.It.IsAny<CommandDomainValidationExceptionResponse>()));
            }
        }
    }

    public class ThrowNotImplementedExceptionCommand : Composable.CQRS.Command.Command {}

    public class ThrowDomainCommandValidationExceptionCommand : Composable.CQRS.Command.Command {}

    public class MyDelegatingToCommandServiceCommandHandler : CommandHandlerBase<ICommand>
    {
        public MyDelegatingToCommandServiceCommandHandler(IServiceBus bus, ICommandService commandService) : base(bus, commandService) { }
    }
}
