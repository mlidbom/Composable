#region usings

using System;
using System.Configuration;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.CQRS.Testing;
using Composable.ServiceBus;
using Composable.System;
using Composable.UnitsOfWork;
using Composable.Windsor;
using JetBrains.Annotations;
using NCrunch.Framework;
using NServiceBus;
using NUnit.Framework;

#endregion

namespace Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests
{
    [TestFixture, NUnit.Framework.Category("NSBFullSetupTests")]
    [ExclusivelyUses(NCrunchExclusivelyUsesResources.NServiceBus)]
    [NCrunch.Framework.Isolated, NUnit.Framework.Category("IgnoreOnTeamCity")]
    public class WhenReceivingMessage
    {
        private TestResults _results;

        [TestFixtureSetUp]
        public void SendMessageToNewEndpoint()
        {
            using(var cloneDomainScope = AppDomain.CurrentDomain.CloneScope())
            {
                _results = cloneDomainScope.CreateType<ReceivingMessageScenario>().Execute();
            }
        }

        [UsedImplicitly]
        public class ReceivingMessageScenario : MarshalByRefObject
        {
            private IServiceBus _bus;
            private IBus _nsbBus;

            public TestResults Execute()
            {
                var endpointConfigurator = new UOWTestEndpointConfigurator("Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests");               

                endpointConfigurator.Init();
                _bus = endpointConfigurator.Container.Resolve<IServiceBus>();
                _nsbBus = endpointConfigurator.Container.Resolve<IBus>();

                var messageHandled = new ManualResetEvent(false);
                var status = TransactionStatus.Active;
                TestingSupportMessageModule.OnHandleBeginMessage += transaction =>
                {
                    transaction.TransactionCompleted += (_, transactionEventArgs) =>
                    {
                        messageHandled.Set();
                        status = transactionEventArgs.Transaction.TransactionInformation.Status;
                    };
                };

                _bus.SendLocal(new InvokeUnitOfWorkCommandMessage());

                Assert.That(messageHandled.WaitOne(30.Seconds()), Is.True, "Timed out waiting for message");

                Assert.That(status, Is.EqualTo(TransactionStatus.Committed), "Message handling did not complete successfully");

                ((IDisposable)_nsbBus).Dispose();
                return new TestResults()
                       {
                           Instances = MyUnitOfWorkParticipant.Instances,
                           TimesCommitted = MyUnitOfWorkParticipant.TimesCommitted,
                           TimesJoined = MyUnitOfWorkParticipant.TimesJoined,
                           TimesRolledBack = MyUnitOfWorkParticipant.TimesRolledBack
                       };
            }
        }

        [Test]
        public void ExactlyOneInstanceOfParticipantIsCreated()
        {
            Assert.That(_results.Instances, Is.EqualTo(1));
        }

        [Test]
        public void ParticipantIsCommittedExactlyOnce()
        {
            Assert.That(_results.TimesCommitted, Is.EqualTo(1));
        }

        [Test]
        public void ParticipantJoinsUowExaclyOnce()
        {
            Assert.That(_results.TimesJoined, Is.EqualTo(1));
        }

        [Test]
        public void ParticipantIsNeverRolledBack()
        {
            Assert.That(_results.TimesRolledBack, Is.EqualTo(0));
        }

    }

    [Serializable]
    public class TestResults
    {
        public int Instances;
        public int TimesCommitted;
        public int TimesRolledBack;
        public int TimesJoined;
    }

    public class InvokeUnitOfWorkCommandMessage : ICommand
    {
    }

    [UsedImplicitly]
    public class MyUnitOfWorkParticipant : IUnitOfWorkParticipant
    {
        public static int Instances;        
        public static int TimesCommitted;
        public static int TimesRolledBack;
        public static int TimesJoined;

        private IUnitOfWork _unit;

        public MyUnitOfWorkParticipant()
        {
            Instances++;
        }

        public IUnitOfWork UnitOfWork { get; private set; }

        public Guid Id { get; private set; }

        public void Join(IUnitOfWork unit)
        {
            TimesJoined++;
            _unit = unit;
        }

        public void Commit(IUnitOfWork unit)
        {
            TimesCommitted++;
            if(_unit != unit)
            {
                throw new Exception("wrong unit!");
            }
        }

        public void Rollback(IUnitOfWork unit)
        {
            TimesRolledBack++;
            if (_unit != unit)
            {
                throw new Exception("wrong unit!");
            }
        }
    }

   
    public class InvokeUOWCommandMessageMessageHandler : IHandleMessages<InvokeUnitOfWorkCommandMessage>
    {
        public InvokeUOWCommandMessageMessageHandler(MyUnitOfWorkParticipant session)
        {
            
        }

        public void Handle(InvokeUnitOfWorkCommandMessage message)
        {
        }
    }

    public class UOWTestEndpointConfigurator : NServicebusEndpointConfigurationBase<UOWTestEndpointConfigurator>, IConfigureThisEndpoint
    {
        private readonly string _queueName;

        public UOWTestEndpointConfigurator(string queueName)
        {
            _queueName = queueName;
        }

        override protected Configure ConfigureLogging(Configure config)
        {
            return config;
        }

        protected override void ConfigureContainer(IWindsorContainer container)
        {
            Container = container;
            container.Register(Component.For<IServiceBus>().ImplementedBy<NServiceBusServiceBus>(),
                Component.For<IUnitOfWorkParticipant, MyUnitOfWorkParticipant>().ImplementedBy<MyUnitOfWorkParticipant>().LifeStyle.PerNserviceBusMessage());
        }

        public IWindsorContainer Container { get; set; }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }
}