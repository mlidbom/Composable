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
using Composable.DomainEvents;
using Composable.GenericAbstractions.Time;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.SystemExtensions.Threading;
using Composable.Windsor;
using NServiceBus;
using NServiceBus.Faults;
using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;
using NServiceBus.UnitOfWork;
using log4net.Config;
using ICommand = Composable.CQRS.Command.ICommand;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public abstract class NServicebusEndpointConfigurationBase<TInheritor> :
        IWantCustomInitialization,
        AsA_NullOpRole
        where TInheritor : IConfigureThisEndpoint
    {
        private WindsorContainer _container;
        private ConfigUnicastBus _serviceBusconfiguration;
        protected static readonly IEnumerable<Assembly> AssembliesItIsRequiredThatYouScan =
            Seq.OfTypes<
                global::NServiceBus.IMessage,//NServiceBus.Interfaces
                global::NServiceBus.Hosting.IHost,//NServiceBus.Host
                Composable.DomainEvents.IDomainEvent,//Composable.DomainEvents
                Composable.CQRS.Command.ICommand,//Composable.CQRS
                Composable.System.Disposable>()//Composable.Core
                .Select(type => type.Assembly)
                .ToList();

        private void StartNServiceBus()
        {
            _serviceBusconfiguration.CreateBus()
                .Start(() => Configure.Instance.ForInstallationOn<global::NServiceBus.Installation.Environments.Windows>().Install());
        }

        protected virtual void ConfigureNServiceBus(WindsorContainer windsorContainer)
        {
            Configure.Serialization.Xml();
            Configure.Transactions.Enable();

            var config = InitializeConfigurationAndDecideOnScanningPolicy()
                .DefineEndpointName(InputQueueName)
                .CastleWindsorBuilder(container: windsorContainer);

            config = SetupConventions(config);

            config = ConfigureLogging(config);


            var config2 = ConfigureSubscriptionStorage(config);
            config2 = ConfigureSaga(config2);

            var busConfig = config2
                .UseTransport<Msmq>()
                .PurgeOnStartup(PurgeOnStartUp)
                .UnicastBus();

            //The empty handlers are kept in order not to break existing overriders and to enable us to insert new handlers here if we need to.
            var busConfig2 = LoadMessageHandlers(busConfig, First<EmptyHandler>.Then<SecondEmptyHandler>());

            //Register our message inspectors
            Configure.Instance.Configurer.ConfigureComponent<CatchSerializationErrorsMessageInspector>(DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<MessageSourceValidator>(DependencyLifecycle.InstancePerCall);

            _serviceBusconfiguration = busConfig2.ImpersonateSender(false);
        }


        protected virtual Configure SetupConventions(Configure config)
        {
            return config.DefiningEventsAs(IsEventType)
                         .DefiningCommandsAs(IsCommandType)
                         .DefiningCommandsAs(IsMessageType);
        }

        protected virtual bool IsMessageType(Type type) => type.IsMessageType();

        protected virtual bool IsCommandType(Type type) => type.IsCommandType();

        protected virtual bool IsEventType(Type type) => type.IsEventType();

        protected virtual Configure InitializeConfigurationAndDecideOnScanningPolicy()
        {
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
                Component.For<IUtcTimeTimeSource>().ImplementedBy<DateTimeNowTimeSource>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer, WindsorContainer>().Instance(_container).LifestyleSingleton(),
                Component.For<IManageUnitsOfWork>().ImplementedBy<ComposableCqrsUnitOfWorkManager>().LifeStyle.PerNserviceBusMessage(),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>().LifeStyle.PerNserviceBusMessage()
                );

            //Forget this and you leak memory like CRAZY!
            _container.Kernel.ReleasePolicy = new NoTrackingReleasePolicy();



            ConfigureNServiceBus(_container);
            ConfigureContainer(_container);
            StartNServiceBus();

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

    // ReSharper disable ClassNeverInstantiated.Global
    public class WillNeverBeUsed : IMessage
    {
    }

    public class EmptyHandler : IHandleMessages<WillNeverBeUsed>
    {
        public void Handle(WillNeverBeUsed message)
        {
        }
    }

    public class SecondEmptyHandler : IHandleMessages<WillNeverBeUsed>
    {
        public void Handle(WillNeverBeUsed message)
        {
        }
    }


    // ReSharper disable InconsistentNaming
    public interface AsA_NullOpRole : IRole //It is apparently now obligatory to use a role so use a fake one...
    // ReSharper restore InconsistentNaming
    {

    }

    public class DoNothingRoleConfigurer : IConfigureRole<AsA_NullOpRole>
    {
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            return Configure.Instance.UnicastBus();
        }
    }

    // ReSharper restore ClassNeverInstantiated.Global
}