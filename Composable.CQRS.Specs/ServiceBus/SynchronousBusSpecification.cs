using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.Command;
using Composable.CQRS.EventSourcing;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
// ReSharper disable UnusedMember.Global

namespace CQRS.Tests.ServiceBus
{
  using Composable.GenericAbstractions.Time;

  public class SynchronousBusSpecification : NSpec.NUnit.nspec
    {
        public void given_no_registered_handlers()
        {
          InProcessServiceBus bus = null;
          IMessageHandlerRegistrar registrar = null;
          before = () =>
                   {
                     var registry = new MessageHandlerRegistry();
                     registrar = registry;
                     bus = new InProcessServiceBus(registry);
                   };

            it["Handles(new ACommand()) returns false"] = () => bus.Handles(new ACommand()).Should().Be(false);
            it["Send(new ACommand()) throws NoHandlerException"] = expect<NoHandlerException>(() => bus.Send(new ACommand()));

            //Todo:reply should throw an exception telling us that you cannot reply except while handling a command
            //it["Reply(new ACommand()) throws CantCallReplyWhenNotHandlingMessageException"] = expect<CantCallReplyWhenNotHandlingMessageException>(() => bus.Reply(new ACommand()));

            it["Publish(new AnEvent()) throws no exception"] = () => bus.Publish(new AnEvent());

            context["after registering a command handler for ACommand with bus"] =
                () =>
                {
                    bool commandHandled = false;
                    before = () =>
                             {
                                registrar.ForCommand<ACommand>(command => commandHandled = true);
                             };
                    it["Handles(new ACommand()) returns true"] = () => bus.Handles(new ACommand()).Should().Be(true);

                    it["Send(new ACommand()) dispatches to registered handler"] = () =>
                                                                               {
                                                                                   bus.Send(new ACommand());
                                                                                   commandHandled.Should().Be(true);
                                                                               };

                };

            context["after registering a handlerfor AnEvent with bus"] =
                () =>
                {
                    bool eventHandled = false;
                    before = () =>
                    {
                        registrar.ForEvent<AnEvent>(command => eventHandled = true);
                    };
                    it["Handles(new AnEvent()) returns true"] = () => bus.Handles(new AnEvent()).Should().Be(true);
                    it["Publish(new AnEvent()) throws no exception"] = () => bus.Publish(new AnEvent());
                    it["Publish(new AnEvent()) dispatches to AnEventHandler"] = () =>
                    {
                        bus.Publish(new AnEvent());
                        eventHandled.Should().Be(true);
                    };

                };         
        }

        public void when_there_is_one_handler_registered_for_a_message()
        {
            InProcessServiceBus bus = null;
          IMessageHandlerRegistrar registrar = null;
          MessageHandlerRegistry registry = null;

            before = () =>
                     {
                       registrar = registry = new MessageHandlerRegistry();
                       bus = new InProcessServiceBus(registry);                         
                         registrar.ForCommand<ACommand>(_ => { });
                     };

            context["when you add another handler for that command that does not implement ISynchronousBusMessageSpy"] = () =>
                           {
                               it["an exception is thrown"] =  () => this.Invoking(_ => registrar.ForCommand<ACommand>(cmd => {})).ShouldThrow<Exception>();
                           };
            

        }

        public class ACommand : ICommand
        {
            public Guid Id { get; } = Guid.NewGuid();
        }

        public class AnEvent : IEvent { }
    }

    
}
