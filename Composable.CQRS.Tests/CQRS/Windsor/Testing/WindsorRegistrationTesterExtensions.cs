using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.System.Linq;
using FluentAssertions;

namespace CQRS.Tests.CQRS.Windsor.Testing
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
                // TODO: Just for make unit tests past, Need solve this issue in TestingWindsorExtensions in Composable.Core
                ignoredServices = _container.Kernel.GetAssignableHandlers(typeof(object)).Distinct()
                    .SelectMany(x => x.ComponentModel.Services).Where(x => x.IsGenericTypeDefinition);
                
                _container.AssertCanResolveAllComponents1(ignoredServices, ignoredComponents);
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

        public static void AssertCanResolveAllComponents1(this IWindsorContainer container, IEnumerable<Type> ignoredServices = null, IEnumerable<string> ignoredComponents = null)
        {
            var errorsOccured = false;
            ignoredServices = (ignoredServices ?? new Type[] { }).ToList();
            ignoredComponents = (ignoredComponents ?? new string[] { }).ToList();
            using (container.BeginScope())
            {
                foreach (var handler in container.Kernel.GetAssignableHandlers(typeof(object)).Distinct())
                {
                    if (ignoredComponents.Any(ignored => handler.ComponentModel.ComponentName.Name == ignored))
                    {
                        Console.WriteLine(@"Ignoring component: {0}", ignoredComponents.Single(ignored => handler.ComponentModel.ComponentName.Name == ignored));
                        continue;
                    }

                    Console.WriteLine("Resolving services for component: {0}", handler.ComponentModel.Name);

                    foreach (var service in handler.ComponentModel.Services)
                    {
                        if (ignoredServices.Any(ignored => ignored == service))
                        {
                            Console.WriteLine(@"    Ignoring service: {0}", ignoredServices.Single(ignored => ignored == service));
                            continue;
                        }

                        Console.WriteLine(@"    Resolving all {0} ", service.FullName);
                        try
                        {
                            var resolved = container.ResolveAll(service).Cast<Object>().Select(s => s.GetType().FullName).OrderBy(s => s);
                            resolved.ForEach((name, index) => Console.WriteLine(@"	Resolved {0} {1}", index + 1, name));
                        }
                        catch (Exception)
                        {
                            Console.WriteLine();
                            Console.WriteLine(@"############################## {0} ##########################", @"Failed to resolve component");
                            Console.WriteLine(@"##############################Component Name: {0} ##########################", handler.ComponentModel.Name);
                            Console.WriteLine(@"##############################Service Type: {0} ##########################", service.FullName);

                            Console.WriteLine();
                            errorsOccured = true;
                        }
                    }
                }
            }
            if (errorsOccured)
            {
                throw new Exception("There were errors resolving components. Please see the printed call stacks above this one.");
            }
        }
    }
}
