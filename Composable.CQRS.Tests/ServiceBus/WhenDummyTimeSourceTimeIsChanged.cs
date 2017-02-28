using System;
using System.Collections.Generic;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.Command;
using Composable.CQRS.EventSourcing;
using Composable.DDD;
using Composable.GenericAbstractions.Time;
using Composable.ServiceBus;
using Composable.System.Reflection;
using Composable.Windsor.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.ServiceBus
{
    using Composable.System;

    [TestFixture]
    public class WhenDummyTimeSourceTimeIsChanged
    {
        IServiceBus _bus;
        DummyTimeSource _timeSource;
        IDisposable _scope;
        WindsorContainer _container;
        ScheduledCommand receivedCommand = null;

        [SetUp]
        public void SetupTask()
        {
            _container = new WindsorContainer();
            receivedCommand = null;

            _container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            _timeSource = DummyTimeSource.FromLocalTime(DateTime.Parse("2015-01-01 10:00"));

            _container.Register(
                Component.For<DummyTimeSource>().Instance(_timeSource),
                Component.For<IServiceBus, IMessageHandlerRegistrar>().ImplementedBy<TestingOnlyServiceBus>().LifestyleScoped()
                );

            _container.ConfigureWiringForTestsCallAfterAllOtherWiring();

            _scope = _container.BeginScope();

            _bus = _container.Resolve<IServiceBus>();
            _container.Resolve<IMessageHandlerRegistrar>()
                      .ForCommand<ScheduledCommand>(cmd => receivedCommand = cmd);
        }

        [Test]
        public void DueMessagesAreDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + 1.Hours(), inOneHour);

            _timeSource.UtcNow = now + 1.Hours();

            receivedCommand.Should()
                           .Be(inOneHour);
        }

        [Test]
        public void NotDueMessagesAreNotDelivered()
        {
            var now = _timeSource.UtcNow;
            var inOneHour = new ScheduledCommand();
            _bus.SendAtTime(now + 1.Hours(), inOneHour);

            _timeSource.UtcNow = now + 1.Minutes();

            receivedCommand.Should()
                           .Be(null);
        }

        [TearDown]
        public void TearDownTask()
        {
            _scope.Dispose();
            _container.Dispose();
        }

        public class ScheduledCommand : Composable.CQRS.Command.Command
        {
        }
    }
}
