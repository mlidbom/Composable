using System;
using Composable.CQRS;
using Composable.CQRS.Command;
using Composable.ServiceBus;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CQRS.Tests.CQRS
{
    public class CommandHandlerBaseSpecification : NSpec.NUnit.nspec
    {
        public void when_executing_command_handler()
        {
            Mock<ICommandService> commandServiceMock = null;
            Mock<IServiceBus> busMock = null;
            MyDelegatingToCommandServiceCommandHandler commandHandler = null;
            before = () =>
                     {
                         commandServiceMock = new Mock<ICommandService>(MockBehavior.Strict);
                         busMock = new Mock<IServiceBus>(MockBehavior.Strict);
                         commandHandler = new MyDelegatingToCommandServiceCommandHandler(busMock.Object, commandServiceMock.Object);
                     };
            context["when commandService throws NotImplementedException"] =
                () =>
                {
                    NotImplementedException exception = null;
                    before = () =>
                             {
                                 commandServiceMock
                                     .Setup(service => service.Execute<ICommand>(It.IsAny<ThrowNotImplementedExceptionCommand>()))
                                     .Throws<NotImplementedException>();

                                 busMock.Setup(bus => bus.Reply(It.IsAny<CommandExecutionExceptionResponse>()));

                                 exception = Assert.Throws<NotImplementedException>(() => commandHandler.Handle(new ThrowNotImplementedExceptionCommand()));
                             };
                    it["NotImplementedException is thrown by command handler"] = () => exception.Should().NotBeNull();
                    it["reply with CommandExecutionExceptionResponse is sent on the bus"] = () => busMock.Verify(bus => bus.Reply(It.IsAny<CommandExecutionExceptionResponse>()));
                };

            context["when commandService throws DomainCommandValidationException"] =
                () =>
                {
                    DomainCommandValidationException exception = null;
                    before = () =>
                             {
                                 commandServiceMock
                                     .Setup(service => service.Execute<ICommand>(It.IsAny<ThrowDomainCommandValidationExceptionCommand>()))
                                     .Callback(() => { throw new DomainCommandValidationException("AMessage"); });
                                 
                                 busMock.Setup(bus => bus.Reply(It.IsAny<CommandDomainValidationExceptionResponse>()));

                                 exception = Assert.Throws<DomainCommandValidationException>(() => commandHandler.Handle(new ThrowDomainCommandValidationExceptionCommand()));
                             };
                    it["DomainCommandValidationException is thrown by command handler"] = () => exception.Should().NotBeNull();
                    it["reply with CommandDomainValidationExceptionResponse is sent on the bus"] = () => busMock.Verify(bus => bus.Reply(It.IsAny<CommandDomainValidationExceptionResponse>()));
                };
        }
    }

    public class ThrowNotImplementedExceptionCommand : Composable.CQRS.Command.Command {}

    public class ThrowDomainCommandValidationExceptionCommand : Composable.CQRS.Command.Command {}

    public class MyDelegatingToCommandServiceCommandHandler : CommandHandlerBase<ICommand>
    {
        public MyDelegatingToCommandServiceCommandHandler(IServiceBus bus, ICommandService commandService) : base(bus, commandService) {}
    }
}
