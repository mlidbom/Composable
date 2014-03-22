using System;
using System.Collections.Generic;
using AccountManagement.Domain.Services;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Composable.CQRS.Windsor.Testing;
using Composable.ServiceBus;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.ContainerInstallers
{     
    public abstract class AccountManagementWiringTest
    {
        //Note that we do NOT test individual classes. We verify that when used as it will really be used things work as expected. 
        //Should we change which installers exist, split installers, merge installers, etc this test will keep working.

        protected IWindsorContainer Container;

        [SetUp]
        public void WireContainer()
        {
            Container = new WindsorContainer();
            Container.ConfigureWiringForTestsCallBeforeAllOtherWiring();            
            Container.Install(
                FromAssembly.Containing<AccountManagement.Domain.ContainerInstallers.AccountManagementDomainEventStoreInstaller>()
            );

            Container.Register(
                Component.For<IServiceBus>().ImplementedBy<SynchronousBus>().LifestylePerWebRequest(),
                Component.For<IWindsorContainer>().Instance(Container)
                );
        }

        [Test]
        public void AllComponentsCanBeResolved()
        {
            Container
                .RegistrationAssertionHelper()
                .AllComponentsCanBeResolved();
        }

        [Test]
        public void EventStoreIsRegisteredScoped()
        {
            Container
                .RegistrationAssertionHelper()
                .LifestyleScoped<IAccountManagementEventStoreSession>();
        }
    }

    [TestFixture]
    public class AccountManagementProductionWiringTest : AccountManagementWiringTest
    {
    }

    [TestFixture]
    public class AccountManagementTestWiringTest : AccountManagementWiringTest
    {
        [SetUp]
        public void SetupTask()
        {
            Container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }

    public static class WindsorRegistrationTesterExtensions
    {
        public class WindsorRegistrationAssertionHelper
        {
            private readonly IWindsorContainer _container;
            public WindsorRegistrationAssertionHelper(IWindsorContainer container)
            {
                _container = container;
            }

            public void AllComponentsCanBeResolved(IEnumerable<Type> ignoredServices = null, IEnumerable<string> ignoredComponents = null)
            {
                _container.AssertCanResolveAllComponents(ignoredServices, ignoredComponents);
            }

            public void LifestyleScoped<TComponent>()
            {
                TComponent firstComponentFromFirstScope;
                TComponent secondComponentFromFirstScope;

                TComponent firstComponentFromSecondScope;
                TComponent secondComponentFromSecondScope;

                using (_container.BeginScope())
                {
                    firstComponentFromFirstScope = _container.Resolve<TComponent>();
                    secondComponentFromFirstScope = _container.Resolve<TComponent>();
                }

                using (_container.BeginScope())
                {
                    firstComponentFromSecondScope = _container.Resolve<TComponent>();
                    secondComponentFromSecondScope = _container.Resolve<TComponent>();
                }


                firstComponentFromFirstScope.Should().Be(secondComponentFromFirstScope, "Two components resolved in the same scope should be the same instance");
                firstComponentFromSecondScope.Should().Be(secondComponentFromSecondScope, "Two components resolved in the same scope should be the same instance");
                
                firstComponentFromFirstScope.Should().NotBe(firstComponentFromSecondScope, "Two components resolved in different scopes should be different instances");
            }
        }

        public static WindsorRegistrationAssertionHelper RegistrationAssertionHelper(this IWindsorContainer me)
        {
            return new WindsorRegistrationAssertionHelper(me);
        }
    }
}