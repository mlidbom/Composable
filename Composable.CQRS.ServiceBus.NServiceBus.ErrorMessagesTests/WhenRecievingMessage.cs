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
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using JetBrains.Annotations;
using NCrunch.Framework;
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
    using Composable.System;

    [TestFixture, NUnit.Framework.Category("NSBFullSetupTests")]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.NServiceBus)]
    [NCrunch.Framework.Isolated]
    public class WhenReceivingMessage
    {
        [Test]
        public void ExceptionPassedToStackTraceFormatterContainsOriginalException()
        {
            AppDomainExtensions.ExecuteInCloneDomainScope<ScenarioExecutor>(
                executor => executor.Execute(), 
                disposeDelay:50.Milliseconds());
        }
    }

    [UsedImplicitly]
    public class ScenarioExecutor : MarshalByRefObject
    {
        public void Execute()
        {
            var endpointConfigurator = new MyEndPointConfigurer("Composable.CQRS.ServiceBus.NServicebus.Tests.ErrorMessages");


            endpointConfigurator.Init();
            var bus = endpointConfigurator.Container.Resolve<IServiceBus>();
            var nsbBus = endpointConfigurator.Container.Resolve<IBus>();

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

            Exception exceptionPassedToFailureHeaderProvider = null;
            var messageErrorHandlingInvoked = new ManualResetEvent(false);

            //endpointConfigurator.Extractor.RecievedException += e =>
            //{
            //    exceptionPassedToFailureHeaderProvider = e;
            //    messageErrorHandlingInvoked.Set();
            //};
            

            bus.SendLocal(new ErrorGeneratingMessage());

            Assert.That(messageHandled.WaitOne(30.Seconds()), Is.True, "Timed out waiting for message");
            
            //Assert.That(messageErrorHandlingInvoked.WaitOne(30.Seconds()), Is.True, "Timed out waiting for error handling to be invoked");
            Thread.Sleep(15000);

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
            try
            {
                try
                {
                    try
                    {
                        ThrowAfterAFewCalls(new RootCauseException());
                    }
                    catch(RootCauseException e)
                    {
                        ThrowAfterAFewCalls(new FirstWrappingException("wrapping1", e));
                    }
                }
                catch(FirstWrappingException e)
                {
                    ThrowAfterAFewCalls(new SecondWrappingException("wrapping1", e));
                }
            }
            catch(SecondWrappingException e)
            {
                ThrowAfterAFewCalls(new ThirdWrappingException("wrapping3", e));
            }
        }

        private static void ThrowAfterAFewCalls(Exception e) 
        {
            ThrowAfterACall(e);
        }

        private static void ThrowAfterACall(Exception e)
        {
            Throw(e);
        }

        private static void Throw(Exception e)
        {
            throw e;
        }
    }

    public class ThirdWrappingException : Exception
    {
        public ThirdWrappingException(string wrapping3, SecondWrappingException secondWrappingException):base(wrapping3, secondWrappingException)
        {
            
        }
    }

    public class SecondWrappingException : Exception
    {
        public SecondWrappingException(string secondWrapping, FirstWrappingException firstWrappingException):base(secondWrapping, firstWrappingException)
        {
            
        }
    }

    public class FirstWrappingException : Exception
    {
        public FirstWrappingException(string outer, RootCauseException rootCauseException):base(outer, rootCauseException)
        {
            
        }
    }

    public class RootCauseException : Exception
    {
        public RootCauseException():base("A root cause message")
        {
            
        }
    }

    public class MyEndPointConfigurer : NServicebusEndpointConfigurationBase<MyEndPointConfigurer>, IConfigureThisEndpoint
    {
        private readonly string _queueName;

        public MyEndPointConfigurer(string queueName)
        {
            _queueName = queueName;
            //Extractor = new ExceptionExtractor();
        }

        override protected bool PurgeOnStartUp { get { return true; } }

        override protected Configure ConfigureLogging(Configure config)
        {
            return config;
        }

        protected override void ConfigureContainer(IWindsorContainer container)
        {
            Container = container;
            container.Register(Component.For<IServiceBus>().ImplementedBy<NServiceBusServiceBus>()
                //,Component.For<IProvideFailureHeaders>().Instance(Extractor)
                );
        }

        //public ExceptionExtractor Extractor { get; private set; }

        public IWindsorContainer Container { get; set; }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }

    //public class ExceptionExtractor : IProvideFailureHeaders
    //{
    //    public event Action<Exception> RecievedException;
    //    public IDictionary<string, string> GetExceptionHeaders(TransportMessage message, Exception e)
    //    {
    //        RecievedException(e);
    //        return  new Dictionary<string, string>();
    //    }
    //}
}