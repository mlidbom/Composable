using System;
using System.Collections.Generic;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.Windsor.Testing;
using FluentAssertions;

namespace Composable.CQRS.Windsor.Testing
{
    public static class WindsorRegistrationTesterExtensions
    {
        public class WindsorRegistrationAssertionHelper
        {
            private readonly IWindsorContainer _container;

            public WindsorRegistrationAssertionHelper(IWindsorContainer container)
            {
                _container = container;
            }

            public WindsorRegistrationAssertionHelper AllComponentsCanBeResolved(IEnumerable<Type> ignoredServices = null, IEnumerable<string> ignoredComponents = null)
            {
                _container.AssertCanResolveAllComponents(ignoredServices, ignoredComponents);
                return this;
            }

            public WindsorRegistrationAssertionHelper LifestyleScoped<TComponent>()
            {
                TComponent firstComponentFromFirstScope;
                TComponent secondComponentFromFirstScope;

                TComponent firstComponentFromSecondScope;
                TComponent secondComponentFromSecondScope;

                using(_container.BeginScope())
                {
                    firstComponentFromFirstScope = _container.Resolve<TComponent>();
                    secondComponentFromFirstScope = _container.Resolve<TComponent>();
                }

                using(_container.BeginScope())
                {
                    firstComponentFromSecondScope = _container.Resolve<TComponent>();
                    secondComponentFromSecondScope = _container.Resolve<TComponent>();
                }


                firstComponentFromFirstScope.Should().Be(secondComponentFromFirstScope, "Two components resolved in the same scope should be the same instance");
                firstComponentFromSecondScope.Should().Be(secondComponentFromSecondScope, "Two components resolved in the same scope should be the same instance");

                firstComponentFromFirstScope.Should().NotBe(firstComponentFromSecondScope, "Two components resolved in different scopes should be different instances");
                return this;
            }
        }

        public static WindsorRegistrationAssertionHelper RegistrationAssertionHelper(this IWindsorContainer me)
        {
            return new WindsorRegistrationAssertionHelper(me);
        }
    }
}
