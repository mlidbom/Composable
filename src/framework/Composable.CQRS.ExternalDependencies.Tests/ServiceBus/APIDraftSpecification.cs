using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        IMessageHandlerRegistrar MessageHandlerRegistrar { get; }
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

        public static class Production
        {
            public static IEndpointHost Create() => new EndpointHost(RunMode.Production);
        }

        public static class Testing
        {
            public static ITestingEndpointHost CreateHost(TestingMode mode = TestingMode.DatabasePool) => new TestingEndpointHost(new RunMode(isTesting: true, mode: mode));
        }

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

            foreach(var otherEndpoint in _endpoints)
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
                var exceptions = _endpoints
                    .SelectMany(endpoint => endpoint.ServiceLocator
                                                    .Resolve<TestingOnlyInterprocessServiceBus>().ThrownExceptions)
                    .ToList();

                if(exceptions.Any())
                {
                    throw new AggregateException("Unhandled exceptions thrown in bus", exceptions.ToArray());
                }

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

            var dummyTimeSource = DummyTimeSource.Now;
            var inprocessBus = new InProcessServiceBus(_registry);
            var testingOnlyServiceBus = new TestingOnlyInterprocessServiceBus(dummyTimeSource, inprocessBus);

            _container.Register(Component.For<ISingleContextUseGuard>()
                                         .ImplementedBy<SingleThreadUseGuard>()
                                         .LifestyleScoped(),
                                Component.For<IEventStoreEventSerializer>()
                                         .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                                         .LifestyleScoped(),
                                Component.For<IUtcTimeTimeSource>()
                                         .UsingFactoryMethod(factoryMethod: _ => DateTimeNowTimeSource.Instance)
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning(),
                                Component.For<IMessageHandlerRegistrar, MessageHandlerRegistry>()
                                         .UsingFactoryMethod(factoryMethod: _ => _registry)
                                         .LifestyleSingleton(),
                                Component.For<IInProcessServiceBus, IMessageSpy>()
                                         .UsingFactoryMethod(_ => inprocessBus)
                                         .LifestyleSingleton(),
                                Component.For<IInterProcessServiceBus, TestingOnlyInterprocessServiceBus>()
                                         .UsingFactoryMethod(factoryMethod: _ => testingOnlyServiceBus)
                                         .LifestyleSingleton());
        }

        public IDependencyInjectionContainer Container => _container;
        public IMessageHandlerRegistrar MessageHandlerRegistrar => _registry;
        public IEndpoint Build() => new Endpoint(_container.CreateServiceLocator());
    }

    class Endpoint : IEndpoint
    {
        public Endpoint(IServiceLocator serviceLocator) => ServiceLocator = serviceLocator;
        public IServiceLocator ServiceLocator { get; }
    }

    public class APIDraftSpecification
    {
        [Fact] async Task SettingUpAHost()
        {
            using(var host = EndpointHost.Testing.CreateHost())
            {
                var commandReceivedGate = ThreadGate.CreateOpenWithTimeout(10.Milliseconds());
                var eventReceivedGate = ThreadGate.CreateOpenWithTimeout(10.Milliseconds());

                host.RegisterEndpoint(endpointBuilder =>
                {
                    endpointBuilder.MessageHandlerRegistrar.RegisterCommandHandler((MyCommand command) =>
                    {
                        commandReceivedGate.AwaitPassthrough();
                        endpointBuilder.Container.CreateServiceLocator().Resolve<IInterProcessServiceBus>().Publish(new MyEvent());
                    });
                    endpointBuilder.MessageHandlerRegistrar.RegisterQueryHandler((MyQuery query) => new QueryResult());
                });

                var clientEndpoint = host.RegisterEndpoint(endpointBuilder => endpointBuilder.MessageHandlerRegistrar.RegisterEventHandler((MyEvent @event) => eventReceivedGate.AwaitPassthrough()));

                var clientBus = clientEndpoint.ServiceLocator.Resolve<IInterProcessServiceBus>();

                clientBus.Send(new MyCommand());

                commandReceivedGate.AwaitPassedThroughCountEqualTo(1);
                eventReceivedGate.AwaitPassedThroughCountEqualTo(1);

                var result = clientBus.Query(new MyQuery());
                result.Should().NotBeNull();

                result = await clientBus.QueryAsync(new MyQuery());
                result.Should().NotBeNull();
            }
        }
    }

    class MyEvent : IEvent {}
    class QueryResult : IQueryResult {}
    class MyQuery : IQuery<QueryResult> {}
    class MyCommand : Command {}
}
