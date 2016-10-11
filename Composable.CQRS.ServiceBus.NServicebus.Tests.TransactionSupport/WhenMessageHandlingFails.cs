#region usings

using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Transactions;
using Castle.Components.DictionaryAdapter;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.CQRS.Testing;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.ServiceBus;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using CQRS.Tests;
using JetBrains.Annotations;
using NCrunch.Framework;
using NServiceBus;
using NUnit.Framework;
using Composable.GenericAbstractions.Time;
using Composable.Windsor;

#endregion

namespace Composable.CQRS.ServiceBus.NServicebus.Tests.TransactionSupport
{
    
    [TestFixture, NUnit.Framework.Category("NSBFullSetupTests")]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.DocumentDbMdf, NCrunchExlusivelyUsesResources.EventStoreDbMdf, NCrunchExlusivelyUsesResources.NServiceBus)]
    [NCrunch.Framework.Isolated]
    public class WhenMessageHandlingFails
    {
        public static readonly string DocumentDbConnectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
        public static readonly string EventStoreConnectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;

        [Test]
        [NCrunch.Framework.Isolated]
        public void StoredEventsAreRemoved()
        {
            var endpointConfigurer = new EndPointConfigurer("Composable.CQRS.ServiceBus.NServicebus.Tests.TransactionSupport");

            var eventStore = new SqlServerEventStore(EventStoreConnectionString, new SingleThreadUseGuard());

            eventStore.ResetDB();
            SqlServerDocumentDb.ResetDB(EventStoreConnectionString);
            SqlServerDocumentDb.ResetDB(DocumentDbConnectionString);

            eventStore.SaveEvents(Aggregate.Create(2).Cast<IEventStored>().SelectMany( agg => agg.GetChanges()));

            endpointConfigurer.Init();
            var messageHandled = new ManualResetEvent(false);
#pragma warning disable 618
            TestingSupportMessageModule.OnHandleBeginMessage += transaction =>
#pragma warning restore 618
                                                                    {
                                                                        transaction.TransactionCompleted += (_, __) => messageHandled.Set();
                                                                    };

            endpointConfigurer.Container.UseComponent<IServiceBus>(
                    bus => bus.SendLocal(new InsertEventsMessage())
                );

            Assert.That(messageHandled.WaitOne(30.Seconds()), Is.True, "Timed out waiting for message");

            using (var tx = new TransactionScope())
            {
                var events = eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize().ToList();
                Assert.That(events, Has.Count.EqualTo(2));
            }
        }
    }

    public class InsertEventsMessage : Composable.ServiceBus.IMessage
    {
    }

    public class Aggregate : AggregateRoot<Aggregate, AggregateRootEvent, IAggregateRootEvent>
    {
        //always the same in order to cause an exception while saving multiple instances. 
        private readonly Guid _aggregateId = Guid.Parse("EFEF768C-F37B-426F-A53B-BF28A254C55E");

        public static Aggregate[] Create(int instances) { return 1.Through(instances).Select(_ => new Aggregate(_)).ToArray(); }

        public Aggregate(int id):base(new DateTimeNowTimeSource())
        {
            RegisterEventAppliers().For<SomeAggregateCreationEvent>(e => SetIdBeVerySureYouKnowWhatYouAreDoing(_aggregateId));

            RaiseEvent(new SomeAggregateCreationEvent(Guid.Parse("00000000-0000-0000-0000-00000000000{0}".FormatWith(id))));
        }
    }

    public class SomeAggregateCreationEvent : AggregateRootEvent, IAggregateRootCreatedEvent
    {
        public SomeAggregateCreationEvent(Guid aggregateRootId):base(aggregateRootId)
        {
            
        }
    }

    [UsedImplicitly]
    public class InseartEventsMessageHandler : global::NServiceBus.IHandleMessages<InsertEventsMessage>
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
        private readonly string _queueName;

        public EndPointConfigurer(string queueName)
        {
            _queueName = queueName;
        }

        protected override void ConfigureContainer(IWindsorContainer container)
        {
            Container = container;
            ConfigureContainerTest(container);
        }

        override protected Configure ConfigureLogging(Configure config)
        {
            return config;
        }

        public static void ConfigureContainerTest(IWindsorContainer container)
        {
            container.Register(
                Component.For<IMessageInterceptor>().Instance(EmptyMessageInterceptor.Instance),
                Component.For<IServiceBus>().ImplementedBy<NServiceBusServiceBus>(),
               
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>()
                    .ImplementedBy<EventStoreSession>()
                    .LifeStyle.PerNserviceBusMessage(),

                Component.For<IEventStore, SqlServerEventStore>().ImplementedBy<SqlServerEventStore>()
                    .DependsOn(Dependency.OnValue(typeof(string), WhenMessageHandlingFails.DocumentDbConnectionString))
                    .LifestyleScoped(),

                Component.For<IDocumentDb>().ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(Dependency.OnValue(typeof(string), WhenMessageHandlingFails.EventStoreConnectionString))
                    .LifestyleScoped(),

                Component.For<IDocumentDbSession, IUnitOfWorkParticipant>().ImplementedBy<DocumentDbSession>()
                    .DependsOn(Dependency.OnValue(typeof(IDocumentDbSessionInterceptor), NullOpDocumentDbSessionInterceptor.Instance))
                    .LifeStyle.PerNserviceBusMessage(),
                Component.For<ISingleContextUseGuard>()
                );
        }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }
}