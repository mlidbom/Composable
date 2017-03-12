using System;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.KeyValueStorage;
using Composable.CQRS.RuntimeTypeGeneration;
using Composable.Persistence.KeyValueStorage;
using Composable.System;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests
{
    [TestFixture]
    public class RuntimeInstanceGeneratorTests_DocumentDb
    {
        //Make the interface nested to ensure that we support that case. Just about every other interface used will not be so I do not test specifically for that here.
        // ReSharper disable MemberCanBePrivate.Global
        public interface IDocumentDbSessionInterface : IDocumentDbSession { }
        public interface IDocumentDbUpdaterInterface : IDocumentDbUpdater { }
        public interface IDocumentDbReaderInterface : IDocumentDbReader { }
        // ReSharper restore MemberCanBePrivate.Global


        [Test]
        public void Can_create_10_documentDbSession_instances_per_millisecond_given_correct_windsor_wiring()
        {
            var container = new WindsorContainer();
            container.Register(
                               Component.For<IDocumentDb>()
                                        .ImplementedBy<InMemoryDocumentDb>()
                                        .LifestyleSingleton(),
                               Component.For<ISingleContextUseGuard>()
                                        .ImplementedBy<SingleThreadUseGuard>()
                                        .LifestyleScoped(),
                               Component.For<IDocumentDbSessionInterceptor>()
                                        .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                                        .LifestyleSingleton()
                              );

            var dbFactory = RuntimeInstanceGenerator.DocumentDb.CreateFactoryMethod<IDocumentDbSessionInterface, IDocumentDbUpdaterInterface, IDocumentDbReaderInterface>();
            //warm up cache
            using (container.BeginScope())
            {
                dbFactory.CreateReader(container);
                dbFactory.CreateUpdater(container);
                dbFactory.CreateSession(container);
            }

            TimeAsserter.Execute(() =>
                                 {
                                     using (container.BeginScope())
                                     {
                                         dbFactory.CreateReader(container);
                                         dbFactory.CreateSession(container);
                                         dbFactory.CreateUpdater(container);
                                     }
                                 },
                                 iterations: 100,
                                 maxTotal: TimeSpanExtensions.Milliseconds(10));
        }

        [Test] public void Throws_exception_if_you_pass_a_built_in_interface()
        {
            //Ensure that it is working if we pass the correct types...
            RuntimeInstanceGenerator.DocumentDb.CreateFactoryMethod<IDocumentDbSessionInterface, IDocumentDbUpdaterInterface, IDocumentDbReaderInterface>();

            this.Invoking(_ => RuntimeInstanceGenerator.DocumentDb.CreateFactoryMethod<IDocumentDbSession, IDocumentDbUpdaterInterface, IDocumentDbReaderInterface>())
                .ShouldThrow<Exception>();

            this.Invoking(_ => RuntimeInstanceGenerator.DocumentDb.CreateFactoryMethod<IDocumentDbSessionInterface, IDocumentDbUpdater, IDocumentDbReaderInterface>())
                .ShouldThrow<Exception>();

            this.Invoking(_ => RuntimeInstanceGenerator.DocumentDb.CreateFactoryMethod<IDocumentDbSessionInterface, IDocumentDbUpdaterInterface, IDocumentDbReader>())
                .ShouldThrow<Exception>();
        }
    }
}