using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Windsor.Testing
{
    [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public static class TestingWindsorExtensions
    {
        public static void ResetTestDataBases(this IWindsorContainer container)
        {
            using(container.BeginScope())
            {
                foreach(var dbResetter in container.ResolveAll<IResetTestDatabases>())
                {
                    dbResetter.ResetDatabase();
                    container.Release(dbResetter);
                }
            }
        }

        /// <summary>
        ///<para>Components registered as PerWebRequest will be remapped to Scoped.</para>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IWindsorContainer container)
        {
            container.Kernel.ComponentModelBuilder.AddContributor(
                new LifestyleRegistrationMutator(originalLifestyle: LifestyleType.PerWebRequest, newLifestyleType: LifestyleType.Scoped)
                );

            container.Register(
                Component.For<ISingleContextUseGuard>()
                    .ImplementedBy<SingleThreadUseGuard>()
                    .LifestyleScoped()
                );
        }

        
        public static void ConfigureWiringForTestsCallAfterAllOtherWiring(this IWindsorContainer container)
        {
            foreach(var configurer in container.ResolveAll<IConfigureWiringForTests>())
            {
                configurer.ConfigureWiringForTesting();
                container.Release(configurer);
            }

            foreach (var configurer in container.ResolveAll<Composable.Windsor.Testing.IConfigureWiringForTests>())
            {
                configurer.ConfigureWiringForTesting();
                container.Release(configurer);
            }
        }

        public static void AssertCanResolveAllComponents(this IWindsorContainer container, IEnumerable<Type> ignoredServices = null, IEnumerable<string> ignoredComponents = null)
        {
            var errorsOccured = false;
            ignoredServices = (ignoredServices ?? new Type[] {}).ToList();
            ignoredComponents = (ignoredComponents ?? new string[] {}).ToList();
            using(container.BeginScope())
            {
                foreach(var handler in container.Kernel.GetAssignableHandlers(typeof(object)).Distinct())
                {
                    if(ignoredComponents.Any(ignored => handler.ComponentModel.ComponentName.Name == ignored))
                    {
                        Console.WriteLine(@"Ignoring component: {0}", ignoredComponents.Single(ignored => handler.ComponentModel.ComponentName.Name == ignored));
                        continue;
                    }

                    Console.WriteLine("Resolving services for component: {0}", handler.ComponentModel.Name);

                    foreach(var service in handler.ComponentModel.Services)
                    {
                        if(ignoredServices.Any(ignored => ignored == service))
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
                        catch(Exception e)
                        {
                            Console.WriteLine();
                            Console.WriteLine(@"############################## {0} ##########################", @"Failed to resolve component");
                            Console.WriteLine(@"##############################Component Name: {0} ##########################", handler.ComponentModel.Name);
                            Console.WriteLine(@"##############################Service Type: {0} ##########################", service.FullName);
                            Console.WriteLine(CreateStackTrace(e));
                            Console.WriteLine();
                            errorsOccured = true;
                        }
                    }
                }
            }
            if(errorsOccured)
            {
                throw new Exception("There were errors resolving components. Please see the printed call stacks above this one.");
            }
        }

        private static string CreateStackTrace(Exception exception)
        {
            return GetNestedExceptionsList(exception)
                .Reverse()
                .Select(currentException => string.Format("Exception:{1}{0}Message:{2}{0}{0}{3}",
                    Environment.NewLine,
                    currentException.GetType().FullName,
                    currentException.Message,
                    currentException.StackTrace))
                .Join(string.Format("{0}   ---End of inner stack trace---{0}{0}", Environment.NewLine));
        }

        private static IEnumerable<Exception> GetNestedExceptionsList(Exception exception)
        {
            yield return exception;
            while(exception.InnerException != null)
            {
                yield return exception.InnerException;
                exception = exception.InnerException;
            }
        }
    }
}
