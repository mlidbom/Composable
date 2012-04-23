#region usings

using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.CQRS.Testing;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.ServiceBus;
using Composable.System;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using NServiceBus;
using NUnit.Framework;
using Composable.CQRS.Windsor;

#endregion

namespace Composable.CQRS.ServiceBus.NServicebus.Tests.TransactionSupport
{
    [TestFixture, Category("NSBFullSetupTests")]
    public class WhenMessageHandlingFails
    {
        public static readonly string DocumentDbConnectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
        public static readonly string EventStoreConnectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;

        [Test]
        public void StoredEventsAreRemoved()
        {
            var endpointConfigurer = new EndPointConfigurer("Composable.CQRS.ServiceBus.NServicebus.Tests.TransactionSupport");

            var eventStoreReader = new SqlServerEventSomethingOrOther(new SqlServerEventStore(EventStoreConnectionString, new DummyServiceBus(new WindsorContainer())));

            SqlServerEventStore.ResetDB(EventStoreConnectionString);
            SqlServerDocumentDb.ResetDB(DocumentDbConnectionString);

            eventStoreReader.SaveEvents(((IEventStored) new Aggregate(2)).GetChanges());

            endpointConfigurer.Init();
            var bus = endpointConfigurer.Container.Resolve<IServiceBus>();

            var messageHandled = new ManualResetEvent(false);
            TestingSupportMessageModule.OnHandleBeginMessage += transaction =>
                                                                    {
                                                                        transaction.TransactionCompleted += (_, __) => messageHandled.Set();
                                                                    };

            bus.SendLocal(new InsertEventsMessage());

            Assert.That(messageHandled.WaitOne(30.Seconds()), Is.True, "Timed out waiting for message");

            using (var tx = new TransactionScope())
            {
                var events = eventStoreReader.StreamEventsAfterEventWithId(null).ToList();
                Assert.That(events, Has.Count.EqualTo(2));
            }
        }
    }

    public class InsertEventsMessage : IMessage
    {
    }

    public class Aggregate : AggregateRoot<Aggregate>
    {
        //always the same in order to cause an exception while saving multiple instances. 
        private readonly Guid _aggregateId = Guid.Parse("EFEF768C-F37B-426F-A53B-BF28A254C55E");

        public Aggregate(int events)
        {
            RegisterEventHandler<SomeEvent>(e => SetIdBeVerySureYouKnowWhatYouAreDoing(_aggregateId));
            1.Through(events).ForEach(i => ApplyEvent(new SomeEvent()));
        }
    }

    public class SomeEvent : AggregateRootEvent
    {
    }

    public class InseartEventsMessageHandler : IHandleMessages<InsertEventsMessage>
    {
        private readonly IEventStoreSession _session;

        public InseartEventsMessageHandler(IEventStoreSession session)
        {
            _session = session;
        }

        public void Handle(InsertEventsMessage message)
        {
            _session.Save(new Aggregate(5));
        }
    }

    public class EndPointConfigurer : NServicebusEndpointConfigurationBase<EndPointConfigurer>, IConfigureThisEndpoint
    {
        public IWindsorContainer Container;
        private string _queueName;

        public EndPointConfigurer(string queueName)
        {
            _queueName = queueName;
        }

        protected override void ConfigureContainer(IWindsorContainer container)
        {
            Container = container;
            ConfigureContainerTest(container);
        }

        public static void ConfigureContainerTest(IWindsorContainer container)
        {
            container.Register(
                Component.For<IMessageInterceptor>().Instance(EmptyMessageInterceptor.Instance),
                Component.For<IServiceBus>().ImplementedBy<NServiceBusServiceBus>(),
                Component.For<IEventStore, SqlServerEventStore>().UsingFactoryMethod(() => new SqlServerEventStore(WhenMessageHandlingFails.EventStoreConnectionString, container.Resolve<IServiceBus>())),
                Component.For<IDocumentDb>().Instance(new SqlServerDocumentDb(WhenMessageHandlingFails.DocumentDbConnectionString)),
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>().UsingFactoryMethod(() => container.Resolve<IEventStore>().OpenSession()).LifeStyle.PerNserviceBusMessage(),
                Component.For<IEventSomethingOrOther, SqlServerEventSomethingOrOther>().ImplementedBy<SqlServerEventSomethingOrOther>().LifeStyle.PerNserviceBusMessage(),
                Component.For<IDocumentDbSession, IUnitOfWorkParticipant>().UsingFactoryMethod(() => container.Resolve<IDocumentDb>().OpenSession()).LifeStyle.PerNserviceBusMessage()
                );
        }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }
}