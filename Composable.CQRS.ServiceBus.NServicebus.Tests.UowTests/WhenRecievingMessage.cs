#region usings

using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Castle.Windsor;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.CQRS.Testing;
using Composable.ServiceBus;
using Composable.System;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using NCrunch.Framework;
using NServiceBus;
using NUnit.Framework;
using Composable.CQRS.Windsor;
using Component = Castle.MicroKernel.Registration.Component;

#endregion

namespace Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests
{
    [TestFixture, NUnit.Framework.Category("NSBFullSetupTests")]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.NServiceBus)]
    [NCrunch.Framework.Isolated]
    public class WhenReceivingMessage
    {
        [TestFixtureSetUp]
        public void SendMessageToNewEndpoint()
        {
            var myDomain = AppDomain.CurrentDomain;
            var otherDomain = AppDomain.CreateDomain("other domain", myDomain.Evidence, myDomain.BaseDirectory, myDomain.RelativeSearchPath, false);

            
            var otherType = typeof(ReceivingMessageScenario);
            var executor = otherDomain.CreateInstanceAndUnwrap(otherType.Assembly.FullName, otherType.FullName) as ReceivingMessageScenario;

            executor.Execute(AppDomain.CurrentDomain);

            AppDomain.Unload(otherDomain);
        }

        public class ReceivingMessageScenario : MarshalByRefObject
        {
            private IServiceBus _bus;
            private IBus _nsbBus;

            public void Execute(AppDomain currentDomain)
            {
                var endpointConfigurer = new MyEndPointConfigurer("Composable.CQRS.ServiceBus.NServicebus.Tests.UowTests");

                 

                ConfigurationManager.OpenExeConfiguration(currentDomain.SetupInformation.ConfigurationFile);


                endpointConfigurer.Init();
                _bus = endpointConfigurer.Container.Resolve<IServiceBus>();
                _nsbBus = endpointConfigurer.Container.Resolve<IBus>();

                var messageHandled = new ManualResetEvent(false);
                TransactionStatus status = TransactionStatus.Active;
                TestingSupportMessageModule.OnHandleBeginMessage += transaction =>
                {
                    transaction.TransactionCompleted += (_, __) =>
                    {
                        messageHandled.Set();
                        status = __.Transaction.TransactionInformation.Status;
                    };
                };

                _bus.SendLocal(new InvokeUOWCommandMessage());

                Assert.That(messageHandled.WaitOne(30.Seconds()), Is.True, "Timed out waiting for message");

                for (int i = 0; i < 100 && status != TransactionStatus.Committed; i++)
                {
                    Console.Write("Awaiting completion");
                    Thread.Sleep(10.Milliseconds());
                }

                Assert.That(status, Is.EqualTo(TransactionStatus.Committed), "Message handling did not complete successfully");

                ((IDisposable)_nsbBus).Dispose();
            }
        }

        [Test]
        public void ExactlyOneInstanceOfParticipantIsCreated()
        {
            Assert.That(MyUOWParticipant.Instances, Is.EqualTo(1));
        }

        [Test]
        public void ParticipantIsCommittedExactlyOnce()
        {
            Assert.That(MyUOWParticipant.TimesCommitted, Is.EqualTo(1));
        }

        [Test]
        public void ParticipantJoinsUowExaclyOnce()
        {
            Assert.That(MyUOWParticipant.TimesJoined, Is.EqualTo(1));
        }

        [Test]
        public void ParticipantIsNeverRolledBack()
        {
            Assert.That(MyUOWParticipant.TimesRollbacked, Is.EqualTo(0));
        }

    }

    public class InvokeUOWCommandMessage : ICommand
    {
    }

    public class MyUOWParticipant : MarshalByRefObject, IUnitOfWorkParticipant
    {
        public static int Instances;        
        public static int TimesCommitted;
        public static int TimesRollbacked;
        public static int TimesJoined;

        private IUnitOfWork _unit;

        public MyUOWParticipant()
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
            TimesRollbacked++;
            if (_unit != unit)
            {
                throw new Exception("wrong unit!");
            }
        }
    }

   
    public class InvokeUOWCommandMessageMessageHandler : IHandleMessages<InvokeUOWCommandMessage>
    {
        public InvokeUOWCommandMessageMessageHandler(MyUOWParticipant session)
        {
            
        }

        public void Handle(InvokeUOWCommandMessage message)
        {
        }
    }

    public class MyEndPointConfigurer : NServicebusEndpointConfigurationBase<MyEndPointConfigurer>, IConfigureThisEndpoint
    {
        private readonly string _queueName;

        public MyEndPointConfigurer(string queueName)
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
                Component.For<IUnitOfWorkParticipant, MyUOWParticipant>().ImplementedBy<MyUOWParticipant>().LifeStyle.PerNserviceBusMessage());
        }

        public IWindsorContainer Container { get; set; }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }
}