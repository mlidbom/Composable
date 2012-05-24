#region usings

using System;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.CQRS.Testing;
using Composable.ServiceBus;
using Composable.System;
using Composable.UnitsOfWork;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;
using Composable.CQRS.Windsor;
using Composable.SystemExtensions;
using System.Linq;
using FluentAssertions;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.ErrorMessagesTests
{
    [TestFixture, Category("NSBFullSetupTests")]
    public class WhenReceivingMessage
    {
        [Test]
        public void ExceptionPassedToStackTraceFormatterContainsOriginalException()
        {
            var endpointConfigurer = new MyEndPointConfigurer("Composable.CQRS.ServiceBus.NServicebus.Tests.ErrorMessages");


            endpointConfigurer.Init();
            var bus = endpointConfigurer.Container.Resolve<IServiceBus>();
            var nsbBus = endpointConfigurer.Container.Resolve<IBus>();

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

            Exception exceptionPassedToFailureHeaderProvider = null;
            ManualResetEvent messageErrorHandlingInvoked = new ManualResetEvent(false);

            endpointConfigurer.Extractor.RecievedException += e =>
            {
                exceptionPassedToFailureHeaderProvider = e;
                messageErrorHandlingInvoked.Set();
            };

            bus.SendLocal(new ErrorGeneratingMessage());            

            Assert.That(messageHandled.WaitOne(30.Seconds()), Is.True, "Timed out waiting for message");

            Thread.Sleep(3000);

            Assert.That(messageErrorHandlingInvoked.WaitOne(30.Seconds()), Is.True, "Timed out waiting for error handling to be invoked");
            
            exceptionPassedToFailureHeaderProvider.GetRootCauseException().Should().BeOfType<RootCauseException>();            

            ((IDisposable)nsbBus).Dispose();

        }     
    }

    public class ErrorGeneratingMessage : ICommand
    {
    }

   
    public class ErrorGeneratingMessageHandler : IHandleMessages<ErrorGeneratingMessage>
    {
        public void Handle(ErrorGeneratingMessage message)
        {
            throw new RootCauseException();
        }
    }

    public class RootCauseException : Exception
    {
    }

    public class MyEndPointConfigurer : NServicebusEndpointConfigurationBase<MyEndPointConfigurer>, IConfigureThisEndpoint
    {
        private readonly string _queueName;

        public MyEndPointConfigurer(string queueName)
        {
            _queueName = queueName;
            Extractor = new ExceptionExtractor();
        }

        protected override void ConfigureContainer(IWindsorContainer container)
        {
            Container = container;
            container.Register(Component.For<IServiceBus>().ImplementedBy<NServiceBusServiceBus>()
                    ,Component.For<IProvideFailureHeaders>().Instance(Extractor)
                );
        }

        public ExceptionExtractor Extractor { get; private set; }

        public IWindsorContainer Container { get; set; }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }

    public class ExceptionExtractor : IProvideFailureHeaders
    {
        public event Action<Exception> RecievedException;
        public IDictionary<string, string> GetExceptionHeaders(TransportMessage message, Exception e)
        {
            RecievedException(e);
            return  new Dictionary<string, string>();
        }
    }
}