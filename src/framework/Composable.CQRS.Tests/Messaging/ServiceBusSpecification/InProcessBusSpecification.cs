using System;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using FluentAssertions;
using Xunit;

// ReSharper disable InconsistentNaming

// ReSharper disable UnusedMember.Global

namespace Composable.Tests.Messaging.ServiceBusSpecification
{
    public class InProcessBusSpecification : IDisposable
    {
        readonly IServiceLocator _container;

        IMessageHandlerRegistrar Registrar => _container.Resolve<IMessageHandlerRegistrar>();
        IInProcessServiceBus Bus => _container.Resolve<IInProcessServiceBus>();

        InProcessBusSpecification() => _container = DependencyInjectionContainer.CreateServiceLocatorForTesting(_ => {});

        public void Dispose() { _container.Dispose(); }

        public class Given_a_bus : InProcessBusSpecification
        {
            public class With_no_registered_handlers : Given_a_bus
            {
                [Fact] public void Send_new_ACommand_throws_an_Exception() => Bus.Invoking(_ => Bus.Send(new ACommand())).ShouldThrow<NoHandlerException>();
                [Fact] public void Get_new_AQuery_throws_an_Exception() => Bus.Invoking(_ => Bus.Send(new ACommand())).ShouldThrow<NoHandlerException>();
                [Fact] public void Publish_new_AnEvent_throws_no_exception() => Bus.Publish(new AnEvent());
            }

            public class With_registered_handler_for_ACommand : Given_a_bus
            {
                bool _commandHandled;
                public With_registered_handler_for_ACommand()
                {
                    _commandHandled = false;
                    Registrar.ForCommand((ACommand command) => { _commandHandled = true; });
                }

                [Fact] public void Sending_new_ACommand_calls_the_handler()
                {
                    Bus.Send(new ACommand());
                    _commandHandled.Should().Be(true);
                }
            }

            public class With_registered_handler_for_AQuery : Given_a_bus
            {
                readonly AQueryResult _aQueryResult;
                public With_registered_handler_for_AQuery()
                {
                    _aQueryResult = new AQueryResult();
                    Registrar.ForQuery((AQuery query) => _aQueryResult);
                }

                [Fact] public void Getting_new_AQuery_returns_the_instance_returned_by_the_handler() => Bus.Query(new AQuery()).Should().Be(_aQueryResult);
            }

            public class With_one_registered_handler_for_AnEvent : Given_a_bus
            {
                bool _eventHandler1Called;
                public With_one_registered_handler_for_AnEvent()
                {
                    _eventHandler1Called = false;
                    Registrar.ForEvent((AnEvent @event) => _eventHandler1Called = true);
                }

                [Fact] public void Publishing_new_AnEvent_calls_the_handler()
                {
                    Bus.Publish(new AnEvent());
                    _eventHandler1Called.Should().BeTrue();
                }
            }

            public class With_two_registered_handlers_for_AnEvent : Given_a_bus
            {
                bool _eventHandler1Called;
                bool _eventHandler2Called;

                public With_two_registered_handlers_for_AnEvent()
                {
                    _eventHandler1Called = false;
                    _eventHandler2Called = false;
                    Registrar.ForEvent((AnEvent @event) => _eventHandler1Called = true);
                    Registrar.ForEvent((AnEvent @event) => _eventHandler2Called = true);
                }

                [Fact] public void Publishing_new_AnEvent_calls_both_handlers()
                {
                    Bus.Publish(new AnEvent());

                    _eventHandler1Called.Should().BeTrue();
                    _eventHandler2Called.Should().BeTrue();
                }
            }
        }

        [TypeId("857392BE-FF1E-45D0-A11F-D5BB0FFC3DCE")]class ACommand : ITransactionalExactlyOnceDeliveryCommand
        {
            public Guid MessageId { get; } = Guid.NewGuid();
        }

        [TypeId("4DB866F4-2FD9-4CEA-832E-2C17FE52450C")]class AQuery : Query<AQueryResult> {}

        class AQueryResult : QueryResult {}

        class AnEvent : AggregateRootEvent {}
    }
}
