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
        IDisposable _scope;

        IMessageHandlerRegistrar Registrar => _container.Resolve<IMessageHandlerRegistrar>();
        ILocalServiceBusSession BusSession => _container.Resolve<ILocalServiceBusSession>();

        InProcessBusSpecification()
        {
            _container = DependencyInjectionContainer.CreateServiceLocatorForTesting(_ => {});
            _scope = _container.BeginScope();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _container.Dispose();
        }

        public class Given_a_bus : InProcessBusSpecification
        {
            public class With_no_registered_handlers : Given_a_bus
            {
                [Fact] public void Send_new_ACommand_throws_an_Exception() => BusSession.Invoking(_ => BusSession.Execute(new ACommand())).ShouldThrow<NoHandlerException>();
                [Fact] public void Get_new_AQuery_throws_an_Exception() => BusSession.Invoking(_ => BusSession.Execute(new ACommand())).ShouldThrow<NoHandlerException>();
                [Fact] public void Publish_new_AnEvent_throws_no_exception() => BusSession.Publish(new AnEvent());
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
                    BusSession.Execute(new ACommand());
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

                [Fact] public void Getting_new_AQuery_returns_the_instance_returned_by_the_handler() => new AQuery().ExecuteOn(BusSession).Should().Be(_aQueryResult);
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
                    BusSession.Publish(new AnEvent());
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
                    BusSession.Publish(new AnEvent());

                    _eventHandler1Called.Should().BeTrue();
                    _eventHandler2Called.Should().BeTrue();
                }
            }
        }

        class ACommand : IExactlyOnceCommand
        {
            public Guid MessageId { get; } = Guid.NewGuid();
        }

        class AQuery : Query<AQueryResult> {}

        class AQueryResult : QueryResult {}

        class AnEvent : AggregateEvent {}
    }
}
