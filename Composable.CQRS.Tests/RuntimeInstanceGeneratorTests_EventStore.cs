using System;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.RuntimeTypeGeneration;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System;
using Composable.SystemExtensions.Threading;
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
            var container = new WindsorContainer();
            container.Register(
                               Component.For<IEventStore>()
                                        .ImplementedBy<InMemoryEventStore>()
                                        .LifestyleSingleton(),
                               Component.For<ISingleContextUseGuard>()
                                        .ImplementedBy<SingleThreadUseGuard>()
                                        .LifestyleScoped(),
                               Component.For<IServiceBus>()
                                        .ImplementedBy<TestingOnlyServiceBus>()
                                        .LifestyleSingleton(),
                               Component.For<IMessageHandlerRegistrar, IMessageHandlerRegistry>()
                                        .ImplementedBy<MessageHandlerRegistry>()
                                        .LifestyleSingleton(),
                               Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                                        .Instance(DummyTimeSource.Now)
                                        .LifestyleSingleton()
                              );

            var dbFactory = RuntimeInstanceGenerator.EventStore.CreateFactoryMethod<IEventStoreSessionInterface, IEventStoreReaderInterface>();
            //warm up cache
            using(container.BeginScope())
            {
                dbFactory.CreateReader(container);
                dbFactory.CreateSession(container);
            }

            TimeAsserter.Execute(() =>
                                 {
                                     using(container.BeginScope())
                                     {
                                         dbFactory.CreateReader(container);
                                         dbFactory.CreateSession(container);
                                     }
                                 },
                                 iterations: 100,
                                 maxTotal: TimeSpanExtensions.Milliseconds(10));
        }

        [Test] public void Throws_exception_if_you_pass_a_built_in_interface()
        {
            //Ensure that it is working if we pass the correct types...
            RuntimeInstanceGenerator.EventStore.CreateFactoryMethod<IEventStoreSessionInterface, IEventStoreReaderInterface>();

            this.Invoking(_ => RuntimeInstanceGenerator.EventStore.CreateFactoryMethod<IEventStoreSession, IEventStoreReaderInterface>())
                .ShouldThrow<Exception>();

            this.Invoking(_ => RuntimeInstanceGenerator.EventStore.CreateFactoryMethod<IEventStoreSessionInterface, IEventStoreReader>())
                .ShouldThrow<Exception>();
        }
    }
}
