using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.CQRS.Tests.ServiceBus
{
    interface IEndpointHost : IDisposable
    {
        IEndpoint RegisterEndpoint(Action<IEndpointBuilder> setup);
    }

    interface ITestingEndpointHost : IEndpointHost {}

    interface IEndpointBuilder
    {
        IDependencyInjectionContainer Container { get; }
        IMessageHandlerRegistrar Registrar { get; }
    }

    interface IEndpoint
    {
        IServiceLocator ServiceLocator { get; }
    }

    class EndpointHost : IEndpointHost
    {
        readonly IRunMode _mode;
        bool _disposed;
        readonly List<IEndpoint> _endpoints = new List<IEndpoint>();

        protected EndpointHost(IRunMode mode) => _mode = mode;

        public static IEndpointHost Create(IRunMode mode) => new EndpointHost(mode);
        public static ITestingEndpointHost CreateForTesting(TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, mode: mode));

        public IEndpoint RegisterEndpoint(Action<IEndpointBuilder> setup)
        {
            var builder = new EndpointBuilder(_mode);

            setup(builder);

            var endpoint = builder.Build();
            ConnectEndpoint(endpoint);

            _endpoints.Add(endpoint);

            return endpoint;
        }

        void ConnectEndpoint(IEndpoint endpoint)
        {
            var currentRegistry = endpoint.ServiceLocator.Resolve<MessageHandlerRegistry>();
            var currentCommandHandlers = currentRegistry._commandHandlers.ToArray();
            var currentQueryHandlers = currentRegistry._queryHandlers.ToArray();
            var currentEventRegistrations = currentRegistry._eventHandlerRegistrations.ToArray();

            foreach (var otherEndpoint in _endpoints)
            {
                var otherRegistry = otherEndpoint.ServiceLocator.Resolve<MessageHandlerRegistry>();

                otherRegistry._commandHandlers.ForEach(handler => currentRegistry._commandHandlers.Add(handler.Key, handler.Value));
                otherRegistry._eventHandlerRegistrations.ForEach(registration => currentRegistry._eventHandlerRegistrations.Add(registration));
                otherRegistry._queryHandlers.ForEach(handler => currentRegistry._queryHandlers.Add(handler.Key, handler.Value));

                currentCommandHandlers.ForEach(handler => otherRegistry._commandHandlers.Add(handler.Key, handler.Value));
                currentEventRegistrations.ForEach(registration => otherRegistry._eventHandlerRegistrations.Add(registration));
                currentQueryHandlers.ForEach(handler => otherRegistry._queryHandlers.Add(handler.Key, handler.Value));

            }
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
            }
        }
    }

    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        public TestingEndpointHost(IRunMode mode) : base(mode) {}
    }

    class EndpointBuilder : IEndpointBuilder
    {
        readonly IDependencyInjectionContainer _container;
        readonly MessageHandlerRegistry _registry;

        public EndpointBuilder(IRunMode mode)
        {
            _container = DependencyInjectionContainer.Create(mode);

            _registry = new MessageHandlerRegistry();

            _container.Register(Component.For<ISingleContextUseGuard>()
                                         .ImplementedBy<SingleThreadUseGuard>()
                                         .LifestyleScoped(),
                                Component.For<IEventStoreEventSerializer>()
                                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                                         .LifestyleScoped(),
                                Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                                         .UsingFactoryMethod(factoryMethod: _ => DateTimeNowTimeSource.Instance)
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning(),
                                Component.For<IMessageHandlerRegistrar, MessageHandlerRegistry>()
                                         .UsingFactoryMethod(factoryMethod: _ => _registry)
                                         .LifestyleSingleton(),
                                Component.For<IServiceBus, IInProcessServiceBus>()
                                         .UsingFactoryMethod(factoryMethod: _ => new TestingOnlyServiceBus(DummyTimeSource.Now, _registry))
                                         .LifestyleSingleton());
        }

        public IDependencyInjectionContainer Container => _container;
        public IMessageHandlerRegistrar Registrar => _registry;
        public IEndpoint Build() => new Endpoint(_container.CreateServiceLocator());
    }

    class Endpoint : IEndpoint
    {
        public Endpoint(IServiceLocator serviceLocator) => ServiceLocator = serviceLocator;
        public IServiceLocator ServiceLocator { get; }
    }

    public class APIDraftSpecification
    {
        [Fact] void SettingUpAHost()
        {
            using(var host = EndpointHost.CreateForTesting())
            {
                var commandReceived = ThreadGate.WithTimeout(10.Milliseconds()).Open();
                var eventReceived = ThreadGate.WithTimeout(10.Milliseconds()).Open();

                host.RegisterEndpoint(builder =>
                                      {
                                          builder.Registrar.ForCommand((MyCommand command) =>
                                                                       {
                                                                           commandReceived.Pass();
                                                                           builder.Container.CreateServiceLocator().Resolve<IServiceBus>().Publish(new MyEvent());
                                                                       });
                                          builder.Registrar.ForQuery((MyQuery query) => new QueryResult());
                                      });

                var clientEndpoint = host.RegisterEndpoint(builder => builder.Registrar.ForEvent((MyEvent @event)=> eventReceived.Pass()));

                var clientBus = clientEndpoint.ServiceLocator.Resolve<IServiceBus>();

                clientBus.Send(new MyCommand());

                commandReceived.AwaitPassedCount(1);

                var result = clientBus.Get(new MyQuery());
                result.Should().NotBeNull();

                eventReceived.AwaitPassedCount(1);
            }
        }
    }

    class MyEvent : IEvent { }
    class QueryResult : IQueryResult {}
    class MyQuery : IQuery<QueryResult> {}
    class MyCommand : Command {}
}
