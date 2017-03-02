using System;
using Composable.Messaging;
using Composable.Messaging.Buses;
using FluentAssertions;

// ReSharper disable UnusedMember.Global

namespace Composable.CQRS.Specs.ServiceBus
{
    public class InProcessBusSpecification : NSpec.NUnit.nspec
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

            it["Handles(new AnEvent()) returns false"] = () => bus.Handles(new AnEvent()).Should().BeFalse();
            it["Publish(new AnEvent()) throws no exception"] = () => bus.Publish(new AnEvent());
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

            it["Handles(new AQuery()) returns false"] = () => bus.Handles(new AQuery()).Should().Be(false);
            it["Get(new AQuery()) throws NoHandlerException"] = expect<NoHandlerException>(() => bus.Get(new AQuery()));
            context["after registering a handler for AQuery with bus"] =
                () =>
                {
                    bool queryHandled = false;
                    before = () =>
                    {
                        Func<AQuery, AQueryResult> queryHandler = query => new AQueryResult();
                        registrar.ForQuery<AQuery, AQueryResult>(query => new AQueryResult());
                    };
                    it["Handles(new AQuery()) returns true"] = () => bus.Handles(new AQuery()).Should().Be(true);
                    it["Get(new AQuery()) throws no exception"] = () => bus.Get(new AQuery());
                    it["Get(new AQuery()) returns an instance of AQueryResult"] = () =>
                                                                                 {
                                                                                     var aQueryResult = bus.Get(new AQuery());
                                                                                     aQueryResult.Should()
                                                                                                 .NotBeNull();
                                                                                     aQueryResult.Should().BeOfType<AQueryResult>();
                                                                                     
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

        class ACommand : ICommand
        {
            public Guid Id { get; } = Guid.NewGuid();
        }

        class AQuery : IQuery<AQueryResult>
        {
            
        }

        class AQueryResult : IQueryResult { }

        class AnEvent : IEvent { }
    }    
}
