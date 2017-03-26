using System;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.System;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenDummyTimeSourceTimeIsChanged
    {
        IServiceBus _bus;
        DummyTimeSource _timeSource;
        IDisposable _scope;
        IDependencyInjectionContainer _container;
        ScheduledCommand _receivedCommand = null;

        [SetUp]
        public void SetupTask()
        {
            _container = DependencyInjectionContainer.Create();
            _receivedCommand = null;

            _container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            _timeSource = DummyTimeSource.FromLocalTime(DateTime.Parse("2015-01-01 10:00"));

          _container.Register(
                              CComponent.For<DummyTimeSource>()
                                       .Instance(_timeSource)
                                       .LifestyleSingleton(),
                              CComponent.For<IMessageHandlerRegistrar, IMessageHandlerRegistry>()
                                       .ImplementedBy<MessageHandlerRegistry>()
                                       .LifestyleSingleton(),
                              CComponent.For<IServiceBus>()
                                       .ImplementedBy<TestingOnlyServiceBus>()
                                       .LifestyleScoped());

            _container.ConfigureWiringForTestsCallAfterAllOtherWiring();

            var serviceLocator = _container.CreateServiceLocator();
            _scope = serviceLocator.BeginScope();

            _bus = serviceLocator.Resolve<IServiceBus>();
            serviceLocator.Resolve<IMessageHandlerRegistrar>()
                      .ForCommand<ScheduledCommand>(cmd => _receivedCommand = cmd);
        }

        [Test]
        public void DueMessagesAreDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + TimeSpanExtensions.Hours(1), inOneHour);

            _timeSource.UtcNow = now + TimeSpanExtensions.Hours(1);

            _receivedCommand.Should()
                           .Be(inOneHour);
        }

        [Test]
        public void NotDueMessagesAreNotDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + TimeSpanExtensions.Hours(1), inOneHour);

            _timeSource.UtcNow = now + TimeSpanExtensions.Minutes(1);

            _receivedCommand.Should()
                           .Be(null);
        }

        [TearDown]
        public void TearDownTask()
        {
            _scope.Dispose();
            _container.Dispose();
        }

        class ScheduledCommand : Command
        {
        }
    }
}
