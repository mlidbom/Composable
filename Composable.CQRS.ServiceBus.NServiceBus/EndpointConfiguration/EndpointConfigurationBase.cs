#region usings

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Releasers;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Unicast.Config;
using NServiceBus.UnitOfWork;
using log4net.Config;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public abstract class NServicebusEndpointConfigurationBase<TInheritor> : IWantCustomInitialization
        where TInheritor : IConfigureThisEndpoint
    {        
        private WindsorContainer _container;

        protected static readonly IEnumerable<Assembly> AssembliesItIsRequiredThatYouScan = Seq.OfTypes<
                global::NServiceBus.IMessage,//NServiceBus.Interfaces
                global::NServiceBus.Hosting.IHost,//NServiceBus.Host

                Composable.DomainEvents.IDomainEvent,//Composable.DomainEvents
                Composable.CQRS.Command.ICommand,//Composable.CQRS
                Composable.DisposeAction>()
                .Select(type => type.Assembly)
                .ToList();//Composable.Core

        protected virtual void StartNServiceBus(WindsorContainer windsorContainer)
        {
            var config = InitializeConfigurationAndDecideOnScanningPolicy()
                .DefineEndpointName(InputQueueName)
                .CastleWindsorBuilder(container: windsorContainer);

            config = ConfigureLogging(config);
                

            var config2 = ConfigureSubscriptionStorage(config);
            config2 = ConfigureSaga(config2);

            var busConfig = config2
                .UseTransport<Msmq>()
                .PurgeOnStartup(PurgeOnStartUp)
                .UnicastBus();

            var busConfig2 = LoadMessageHandlers(busConfig, First<EmptyHandler>.Then<MessageSourceValidator>().AndThen<CatchSerializationErrors>());

            busConfig2.ImpersonateSender(false)
                .CreateBus()
                .Start(() => Configure.Instance.ForInstallationOn<global::NServiceBus.Installation.Environments.Windows>().Install());
        }

        protected virtual Configure InitializeConfigurationAndDecideOnScanningPolicy()
        {
            Configure.Serialization.Xml();
            Configure.Transactions.Enable();

            return Configure.With();
        }

        protected virtual Configure InitializeConfigurationScanningAssembliesContainingTheseTypes(IEnumerable<Type> typesThatSelectAssemblies)
        {
            return InitializeConfigurationScanningTheseAssemblies(typesThatSelectAssemblies.Select(type => type.Assembly).ToSet());
        }

        protected virtual Configure InitializeConfigurationScanningTheseAssemblies(IEnumerable<Assembly> assembliesToScan)
        {
            return Configure.With(AssembliesItIsRequiredThatYouScan.Concat(assembliesToScan).ToSet());
        }

        protected virtual bool PurgeOnStartUp { get { return false; } }

        protected virtual Configure ConfigureLogging(Configure config)
        {
            return config.Log4Net();
        }

        protected abstract string InputQueueName { get; }

        protected virtual ConfigUnicastBus LoadMessageHandlers(ConfigUnicastBus busConfig, First<EmptyHandler> required)
        {
            var busConfig2 = busConfig.LoadMessageHandlers(required);
            return busConfig2;
        }

        protected virtual Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.UseNHibernateSubscriptionPersister();
        }

        protected virtual Configure ConfigureSaga(Configure config)
        {
            return config;
        }


        public void Init()
        {
            XmlConfigurator.Configure();
            _container = new WindsorContainer();

            _container.Kernel.ComponentModelBuilder.AddContributor(
                new LifestyleRegistrationMutator(
                    LifestyleType.PerWebRequest,
                    LifestyleType.Scoped));

            _container.Register(
                Component.For<IWindsorContainer, WindsorContainer>().Instance(_container),
                Component.For<IManageUnitsOfWork>().ImplementedBy<ComposableCqrsUnitOfWorkManager>().LifeStyle.PerNserviceBusMessage(),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>().LifeStyle.PerNserviceBusMessage()
                );

            //Forget this and you leak memory like CRAZY!
            _container.Kernel.ReleasePolicy = new NoTrackingReleasePolicy();

            ConfigureContainer(_container);

            StartNServiceBus(_container);

            AssertContainerConfigurationValid();
        }

        protected virtual void AssertContainerConfigurationValid()
        {
            _container.AssertConfigurationValid();
        }

        protected abstract void ConfigureContainer(IWindsorContainer container);

        protected static string GetConnectionStringFromConfiguration(string key)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[key];
            if (connectionString == null)
                throw new ConfigurationErrorsException(string.Format("Missing connectionstring for '{0}'", key));
            return connectionString.ConnectionString;
        }
    }

    public class WillNeverBeUsed : IMessage
    {
    }

    public class EmptyHandler : IHandleMessages<WillNeverBeUsed>
    {
        public void Handle(WillNeverBeUsed message)
        {
        }
    }
}