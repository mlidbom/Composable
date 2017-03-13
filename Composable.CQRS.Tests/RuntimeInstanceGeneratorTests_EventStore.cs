using System;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.CQRS.Windsor;
using Composable.CQRS.RuntimeTypeGeneration;
using Composable.CQRS.Windsor.Testing.Testing;
using Composable.System;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests
{
    [TestFixture] public class RuntimeInstanceGeneratorTests_EventStore
    {
        //Make the interface nested to ensure that we support that case. Just about every other interface used will not be so I do not test specifically for that here.
        // ReSharper disable MemberCanBePrivate.Global
        public interface IEventStoreSessionInterface : IEventStoreSession {}

        public interface IEventStoreReaderInterface : IEventStoreReader {}
        // ReSharper restore MemberCanBePrivate.Global

        [Test] public void Can_create_10_EventStore_instances_per_millisecond_given_correct_windsor_wiring()
        {
            using(var container = CreateContainerWithDependencies())
            {

                //warm up cache
                using(container.BeginScope())
                {
                    container.Resolve<IEventStoreSessionInterface>();
                    container.Resolve<IEventStoreReaderInterface>();
                }

                TimeAsserter.Execute(() =>
                                     {
                                         using(container.BeginScope())
                                         {
                                             container.Resolve<IEventStoreSessionInterface>();
                                             container.Resolve<IEventStoreReaderInterface>();
                                         }
                                     },
                                     iterations: 100,
                                     maxTotal: TimeSpanExtensions.Milliseconds(10));
            }
        }
        static IWindsorContainer CreateContainerWithDependencies()
        {
            var container = new WindsorContainer()
                .SetupForTesting(_ => _.RegisterSqlServerEventStore<IEventStoreSessionInterface, IEventStoreReaderInterface>("ignored connection string"));
            return container;
        }

        [Test] public void Throws_exception_if_you_pass_a_built_in_interface()
        {
            //Ensure that it is working if we pass the correct types...
            RuntimeInstanceGenerator.EventStore.CreateType<IEventStoreSessionInterface, IEventStoreReaderInterface>();

            this.Invoking(_ => RuntimeInstanceGenerator.EventStore.CreateType<IEventStoreSession, IEventStoreReaderInterface>())
                .ShouldThrow<Exception>();

            this.Invoking(_ => RuntimeInstanceGenerator.EventStore.CreateType<IEventStoreSessionInterface, IEventStoreReader>())
                .ShouldThrow<Exception>();
        }
    }
}
